using System.Collections.ObjectModel;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Estado del flujo Premios y Reintegros (Gestión, mockups 3.x) contra la
/// API real: histórico (GET api/mobile/premios-reintegros), guardado de la
/// captura (POST, folio servidor PYR-YYYYMMDD-NNNN — D3) y detalle agrupado
/// por sorteo (GET /{id} — D2).
///
/// Reglas D1-D3 (aprobadas 2026-10-13):
///   - D1: SOLO premios y reintegros ACEPTADOS se registran; "NO TIENE
///     PREMIO" no se acumula (la captura lo descarta al ACEPTAR).
///   - D2: el detalle se persiste por boleto en el backend y la vista
///     agrupada por sorteo se reconstruye desde él.
///   - D3: folio del backend (PYR-YYYYMMDD-NNNN), nunca local.
///
/// La CAPTURA en curso sigue en memoria (BoletoCapturado) porque es el
/// estado vivo del escáner continuo; al Guardar se envía al backend y el
/// histórico se recarga del GET. El escáner en sí (POST
/// api/mobile/premios/consultar via ConsultaPremiosService) NO cambia.
///
/// ANTES: lista sembrada con 1 movimiento de ejemplo + folio incremental
/// local (fase solo-interfaz). El movimiento de ejemplo fue ELIMINADO.
/// </summary>
public sealed class PremiosReintegrosService
{
    private readonly IApiClient _api;

    public PremiosReintegrosService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>Movimientos del histórico (se recargan del backend).</summary>
    public ObservableCollection<MovimientoPremiosReintegros> Movimientos { get; } = new();

    /// <summary>Captura en curso (estado vivo del escáner continuo).</summary>
    public ObservableCollection<BoletoCapturado> CapturaActual { get; } = new();

    // ================= Resumen de la lista (agregado de movimientos) =================

    public decimal TotalPremios => Movimientos.Sum(m => m.TotalPremios);

    public decimal TotalReintegros => Movimientos.Sum(m => m.TotalReintegros);

    public decimal TotalGeneral => TotalPremios + TotalReintegros;

    // ================= Captura en curso =================

    public int ContadorPremios => CapturaActual.Count(b => b.Tipo == TipoCapturaBoleto.Premio);

    public int ContadorReintegros => CapturaActual.Count(b => b.Tipo == TipoCapturaBoleto.Reintegro);

    public decimal MontoPremios => CapturaActual.Where(b => b.Tipo == TipoCapturaBoleto.Premio).Sum(b => b.Valor);

    public decimal MontoReintegros => CapturaActual.Where(b => b.Tipo == TipoCapturaBoleto.Reintegro).Sum(b => b.Valor);

    /// <summary>Dedupe: el mismo codigo no se cuenta dos veces en la captura.</summary>
    public bool YaCapturado(string codigoCompleto) =>
        CapturaActual.Any(b => b.CodigoCompleto == codigoCompleto);

    /// <summary>Acumula un boleto escaneado en la captura en curso.</summary>
    public void AgregarBoleto(BoletoCapturado boleto)
    {
        CapturaActual.Add(boleto);
    }

    /// <summary>
    /// Carga el histórico real del billetero (GET). Devuelve true si OK.
    /// Los errores se propagan como ApiException/HttpRequestException al VM.
    /// </summary>
    public async Task<bool> CargarMovimientosAsync(CancellationToken cancellationToken = default)
    {
        List<MovimientoPyRApi>? items = await _api
            .GetAsync<List<MovimientoPyRApi>>(ApiEndpoints.MobilePremiosReintegros.Historial, cancellationToken)
            .ConfigureAwait(false);

        Movimientos.Clear();
        foreach (MovimientoPyRApi item in items ?? new List<MovimientoPyRApi>())
        {
            Movimientos.Add(Mapear(item));
        }
        return true;
    }

    /// <summary>
    /// Guardar (D3): envía la captura al backend (POST), que asigna el
    /// folio PYR-YYYYMMDD-NNNN y los totales; limpia la captura y agrega
    /// el movimiento nuevo al histórico local. Sin conexión NO se limpia
    /// la captura (el billetero puede reintentar).
    /// </summary>
    public async Task<MovimientoPremiosReintegros> GuardarCapturaAsync(CancellationToken cancellationToken = default)
    {
        var boletos = CapturaActual.Select(b => new
        {
            tipo = b.Tipo == TipoCapturaBoleto.Premio ? "premio" : "reintegro",
            codigo_barras = b.CodigoCompleto,
            numero_billete = b.NumeroBillete,
            serie = b.Tipo == TipoCapturaBoleto.Reintegro ? null : (b.SignoOSerie != "-" ? b.SignoOSerie : null),
            signo = b.Tipo == TipoCapturaBoleto.Premio && b.SignoOSerie != "-" ? b.SignoOSerie : null,
            fraccion = int.TryParse(b.Vig, out int fr) ? fr : (int?)null,
            nombre_sorteo = b.NombreSorteo,
            id_sorteo = (int?)null,
            valor = b.Valor,
        }).ToList();

        var body = new { boletos };

        MovimientoCreadoPyRApi? creado = await _api
            .PostAsync<MovimientoCreadoPyRApi>(ApiEndpoints.MobilePremiosReintegros.Guardar, body, cancellationToken)
            .ConfigureAwait(false);

        if (creado is null)
        {
            throw new ApiException(
                System.Net.HttpStatusCode.OK,
                "El servidor no devolvió el movimiento guardado");
        }

        var movimiento = new MovimientoPremiosReintegros
        {
            IdMovimiento = creado.IdMovimiento,
            FolioTexto = creado.Folio,
            Fecha = FechaFormatoMockup(creado.FechaMovimiento),
            TotalPremiosApi = creado.TotalPremios,
            TotalReintegrosApi = creado.TotalReintegros,
        };

        Movimientos.Insert(0, movimiento);
        CapturaActual.Clear();
        return movimiento;
    }

    /// <summary>
    /// Detalle agrupado por sorteo (D2) de un movimiento (GET /{id}).
    /// </summary>
    public async Task<DetallePyRApi> CargarDetalleAsync(int idMovimiento, CancellationToken cancellationToken = default)
    {
        DetallePyRApi? detalle = await _api
            .GetAsync<DetallePyRApi>(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, ApiEndpoints.MobilePremiosReintegros.Detalle, idMovimiento),
                cancellationToken)
            .ConfigureAwait(false);

        return detalle ?? throw new ApiException(
            System.Net.HttpStatusCode.OK,
            "El servidor no devolvió el detalle del movimiento");
    }

    /// <summary>Cancelar (mantener presionado): descarta la captura en curso.</summary>
    public void CancelarCaptura()
    {
        CapturaActual.Clear();
    }

    // ================= Mapeos =================

    /// <summary>Fila del histórico → entidad de la lista.</summary>
    private static MovimientoPremiosReintegros Mapear(MovimientoPyRApi item)
    {
        return new MovimientoPremiosReintegros
        {
            IdMovimiento = item.IdMovimiento,
            FolioTexto = item.Folio,
            Fecha = FechaFormatoMockup(item.FechaMovimiento),
            TotalPremiosApi = item.TotalPremios,
            TotalReintegrosApi = item.TotalReintegros,
        };
    }

    /// <summary>"yyyy-MM-dd HH:mm:ss" del backend → "DD-mes-AAAA" del mockup.</summary>
    private static string FechaFormatoMockup(string? cruda)
    {
        if (Helpers.FormatosFecha.TryParseFechaBackend(cruda, out DateOnly fecha))
        {
            return Helpers.FormatosFecha.FechaLarga(fecha);
        }
        return cruda ?? string.Empty;
    }
}
