using MiCachito.Mobile.Api;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Fuente del reporte de Fondo de Ahorro (pestaña 2 de Reportes, mockup
/// 9.2) contra la API real: GET api/mobile/reportes/fondo-ahorro con el
/// rango del modal "Seleccionar Fechas". Todo queda acotado al billetero
/// de la sesión (token); el endpoint no acepta ids del cliente.
///
/// El backend entrega los montos de movimientos SIEMPRE positivos y el
/// signo se deriva aquí del origen (aportacion | retiro), como indica el
/// contrato del endpoint. Lanza ApiException en errores HTTP; la red
/// caída llega como HttpRequestException/TaskCanceled — el VM la captura
/// y muestra "sin conexión" sin inventar datos.
/// </summary>
public class FondoAhorroService
{
    private const string Ruta = "api/mobile/reportes/fondo-ahorro";

    private readonly IApiClient _api;

    public FondoAhorroService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// Consulta el histórico del periodo indicado y lo mapea a la entidad
    /// de dominio (saldos + movimientos con signo, cronológicos).
    /// </summary>
    public async Task<FondoAhorro?> ObtenerAsync(DateTime inicio, DateTime fin, CancellationToken cancellationToken = default)
    {
        string path = $"{Ruta}?fecha_inicio={inicio:yyyy-MM-dd}&fecha_fin={fin:yyyy-MM-dd}";
        FondoAhorroApi? api = await _api.GetAsync<FondoAhorroApi>(path, cancellationToken)
            .ConfigureAwait(false);
        if (api is null)
        {
            return null;
        }

        var fondo = new FondoAhorro
        {
            FechaInicio = inicio,
            FechaFin = fin,
            Titular = api.Titular,
            SaldoInicial = api.SaldoInicial,
            Depositos = api.Depositos,
            Retiros = api.Retiros,
            SaldoFinal = api.SaldoFinal,
        };

        foreach (MovimientoFondoAhorroApi m in api.Movimientos ?? new List<MovimientoFondoAhorroApi>())
        {
            // Fecha cruda del backend → DateOnly es-MX seguro (patrón Recibos).
            if (!FormatosFecha.TryParseFechaBackend(m.Fecha, out DateOnly fecha))
            {
                continue; // sin fecha no hay fila confiable — no inventar
            }

            bool esRetiro = m.Origen == "retiro";
            fondo.Movimientos.Add(new MovimientoFondoAhorro
            {
                Fecha = fecha.ToDateTime(TimeOnly.MinValue),
                Folio = m.Folio,
                Origen = m.Origen,
                Monto = esRetiro ? -m.Monto : m.Monto,
                Descripcion = m.Descripcion,
            });
        }

        return fondo;
    }
}
