using MiCachito.Mobile.Api;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Servicio de la pantalla Sorteos (mockups 4, 4.1, 4.2) contra la API
/// real: GET api/mobile/sorteos (lista de sorteos registrados del
/// billetero de la sesión, UNION movimientos + cartera) y GET
/// api/mobile/sorteos/{id} (detalle: entregas/devoluciones por folio,
/// pagos con aplicado_a y resumen con saldo).
///
/// FASE 2 INTEGRACIÓN (2026-09-23): reemplaza el catálogo local de
/// fase interfaz. Todo queda acotado al billetero de la sesión (token);
/// los endpoints no aceptan ids del cliente. Las filas sin fecha se
/// descartan (no se inventa). Lanza ApiException en errores HTTP; la
/// red caída llega como HttpRequestException/TaskCanceled — el VM la
/// captura y muestra el error sin inventar datos.
/// </summary>
public class SorteosService
{
    private const string RutaLista = "api/mobile/sorteos";

    private readonly IApiClient _api;

    public SorteosService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// Sorteos registrados del billetero de la sesión, con label,
    /// fecha y agregados (fecha descendente, como llega del backend).
    /// </summary>
    public async Task<IReadOnlyList<SorteoCelebradoInfo>> SorteosRegistradosAsync(
        CancellationToken cancellationToken = default)
    {
        SorteosBilleteroApi? api = await _api.GetAsync<SorteosBilleteroApi>(RutaLista, cancellationToken)
            .ConfigureAwait(false);

        var lista = new List<SorteoCelebradoInfo>();
        if (api?.Sorteos is null)
        {
            return lista;
        }

        foreach (SorteoBilleteroApi s in api.Sorteos)
        {
            // Fecha cruda del backend → DateOnly es-MX seguro (patrón FA/FAC/EC).
            if (!FormatosFecha.TryParseFechaBackend(s.Fecha, out DateOnly fecha))
            {
                continue; // sin fecha no hay fila confiable — no inventar
            }

            lista.Add(new SorteoCelebradoInfo
            {
                IdSorteo = s.IdSorteo,
                NumeroSorteo = string.IsNullOrWhiteSpace(s.NumeroSorteo) ? null : s.NumeroSorteo,
                Sorteo = s.Sorteo,
                Fecha = fecha.ToDateTime(TimeOnly.MinValue),
                Consignado = s.Consignado,
                Devuelto = s.Devuelto,
                Ventas = s.Consignado - s.Devuelto,
                PagosRealizados = s.Pagado,
                SaldoAPagar = s.Saldo,
            });
        }

        return lista;
    }

    /// <summary>
    /// Detalle real de la dotación indicada: entregas/devoluciones por
    /// folio, pagos realizados (con aplicado_a) y resumen con saldo y
    /// pagos al momento del sorteo. El numero_sorteo de recepción
    /// identifica la dotación (fila sin número = cartera).
    /// </summary>
    public async Task<SorteoCelebradoInfo?> ObtenerDetalleAsync(
        int idSorteo, string? numeroSorteo = null, CancellationToken cancellationToken = default)
    {
        // Query param numero_sorteo SOLO cuando la fila tiene dotación
        // (sin el param el backend devuelve la fila "sin número").
        string path = string.IsNullOrWhiteSpace(numeroSorteo)
            ? $"{RutaLista}/{idSorteo}"
            : $"{RutaLista}/{idSorteo}?numero_sorteo={Uri.EscapeDataString(numeroSorteo)}";

        DetalleSorteoApi? api = await _api.GetAsync<DetalleSorteoApi>(path, cancellationToken)
            .ConfigureAwait(false);
        if (api is null)
        {
            return null;
        }

        ResumenSorteoApi? r = api.Resumen;
        var info = new SorteoCelebradoInfo
        {
            IdSorteo = api.IdSorteo,
            NumeroSorteo = string.IsNullOrWhiteSpace(api.NumeroSorteo) ? null : api.NumeroSorteo,
            Sorteo = api.Sorteo,
            Consignado = r?.Consignado ?? 0m,
            Devuelto = r?.Devuelto ?? 0m,
            Ventas = r?.Ventas ?? 0m,
            PagosRealizados = r?.Pagos ?? 0m,
            SaldoAPagar = r?.Saldo ?? 0m,
            PagosAlMomento = r?.PagosAlMomento ?? 0m,
            Entregas = MapearEntregas(api.EntregasDevoluciones),
            Pagos = MapearPagos(api.PagosRealizados),
        };

        return info;
    }

    /// <summary>Mapea los folios DTO → entidad (fecha es-MX corta).</summary>
    private static IReadOnlyList<FolioEntregaSorteo> MapearEntregas(List<FolioEntregaApi>? filas)
    {
        var entregas = new List<FolioEntregaSorteo>();
        foreach (FolioEntregaApi f in filas ?? new List<FolioEntregaApi>())
        {
            // Fecha cruda → "dd-MM-yyyy" para la tarjeta.
            string fecha = FormatosFecha.TryParseFechaBackend(f.Fecha, out DateOnly d)
                ? d.ToString("dd-MM-yyyy")
                : string.Empty;

            entregas.Add(new FolioEntregaSorteo
            {
                Folio = f.Folio,
                Movimiento = f.Movimiento,
                Fecha = fecha,
                Series = f.Series,
                Subtotal = f.Subtotal,
                Isr = f.RetIsr,
                Comision = f.Comision,
                Fda = f.FDeA,
                Total = f.Total,
            });
        }
        return entregas;
    }

    /// <summary>Mapea los pagos DTO → entidad (fecha es-MX corta).</summary>
    private static IReadOnlyList<PagoSorteo> MapearPagos(List<PagoSorteoApi>? filas)
    {
        var pagos = new List<PagoSorteo>();
        foreach (PagoSorteoApi p in filas ?? new List<PagoSorteoApi>())
        {
            string fecha = FormatosFecha.TryParseFechaBackend(p.Fecha, out DateOnly d)
                ? d.ToString("dd-MM-yyyy")
                : string.Empty;

            pagos.Add(new PagoSorteo
            {
                Fecha = fecha,
                Folio = p.Folio,
                Monto = p.Monto,
                Tipo = p.Tipo,
                AplicadoA = p.AplicadoA,
            });
        }
        return pagos;
    }
}
