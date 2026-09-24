using MiCachito.Mobile.Api;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Fuente del reporte Estado de Cuenta (pestaña 1 de Reportes, mockup
/// 9.1) contra la API real: GET api/mobile/reportes/estado-cuenta (sin
/// parámetros — el backend toma SIEMPRE el billetero de la sesión).
///
/// FASE 4 (2026-09-22): reemplaza la estructura en cero de fase interfaz.
/// El endpoint entrega conceptos y totales YA calculados (fórmulas
/// validadas en Fase 1) y una fila por sorteo con SOLO consignaciones
/// vivas; las filas sin fecha se descartan (no se inventan). Lanza
/// ApiException en errores HTTP; la red caída llega como
/// HttpRequestException/TaskCanceled — el VM la captura y muestra el
/// aviso sin inventar datos.
/// </summary>
public class EstadoCuentaService
{
    private const string Ruta = "api/mobile/reportes/estado-cuenta";

    private readonly IApiClient _api;

    public EstadoCuentaService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// Consulta el estado de cuenta del billetero de la sesión y lo
    /// mapea a la entidad de dominio (resumen + filas + totales).
    /// </summary>
    public async Task<EstadoCuenta?> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        EstadoCuentaApi? api = await _api.GetAsync<EstadoCuentaApi>(Ruta, cancellationToken)
            .ConfigureAwait(false);
        if (api is null)
        {
            return null;
        }

        var estado = new EstadoCuenta
        {
            Vendedor = api.Billetero?.Nombre ?? string.Empty,
            Plaza = api.Billetero?.Cedis ?? string.Empty,
            AlCorriente = api.AlCorriente,
            FondoDeAhorro = api.Conceptos?.FondoDeAhorro ?? 0m,
            Fideicomiso = api.Conceptos?.Fideicomiso ?? 0m,
            Pagares = api.Conceptos?.Pagares ?? 0m,
            BolsaElectronica = api.Conceptos?.BolsaElectronica ?? 0m,
            GarantiaTotal = api.Conceptos?.GarantiaTotal ?? 0m,
            CapacidadDeCredito = api.Conceptos?.CapacidadDeCredito ?? 0m,
            VencidoTotal = api.Totales?.Vencido ?? 0m,
            ConsignaTotal = api.Totales?.Consigna ?? 0m,
            PagosTotal = api.Totales?.Pagos ?? 0m,
            TotalFinal = api.Totales?.Total ?? 0m,
        };

        // Fecha de emisión cruda del backend → fecha segura es-MX.
        if (FormatosFecha.TryParseFechaBackend(api.FechaEmision, out DateOnly emision))
        {
            estado.FechaEmision = emision.ToDateTime(TimeOnly.MinValue);
        }

        foreach (SorteoEstadoCuentaApi s in api.Sorteos ?? new List<SorteoEstadoCuentaApi>())
        {
            // Fecha cruda del backend → DateOnly es-MX seguro (patrón FA/FAC).
            if (!FormatosFecha.TryParseFechaBackend(s.Fecha, out DateOnly fecha))
            {
                continue; // sin fecha no hay fila confiable — no inventar
            }

            estado.Sorteos.Add(new SorteoEstadoCuenta
            {
                Sorteo = s.Sorteo,
                Fecha = fecha.ToDateTime(TimeOnly.MinValue),
                Cantidad = s.Cantidad,
                Vencido = s.Vencido,
                Consigna = s.Consigna,
                Pagos = s.Pagos,
                Total = s.Total,
            });
        }

        return estado;
    }
}
