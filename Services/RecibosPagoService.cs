using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Recibos de Pago del billetero contra la API real (/api/mobile/pagos/*).
///
/// El backend devuelve únicamente fichas_pago con estatus 'aplicado' (ya
/// aplicadas por Caja desde Escritorio); pendientes/rechazadas/canceladas
/// NO son recibos. Todo queda acotado al billetero de la sesión (token).
///
/// Lanza ApiException en errores HTTP; la red caída llega como
/// HttpRequestException/TaskCanceled — el VM la captura y muestra
/// "sin conexión" sin inventar datos.
/// </summary>
public class RecibosPagoService
{
    private const string RutaRecibos = "api/mobile/pagos/recibos";
    private const string RutaRecibo = "api/mobile/pagos/recibo";

    private readonly IApiClient _api;

    public RecibosPagoService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>Lista de recibos aplicados, cronológico inverso (mockup 8).</summary>
    public async Task<List<ReciboPagoItemApi>> CargarRecibosAsync(CancellationToken cancellationToken = default)
    {
        List<ReciboPagoItemApi>? items = await _api.GetAsync<List<ReciboPagoItemApi>>(RutaRecibos, cancellationToken)
            .ConfigureAwait(false);
        return items ?? new List<ReciboPagoItemApi>();
    }

    /// <summary>Detalle de un recibo (mockups 8.1/8.3): ficha + desglose + documentos pagados.</summary>
    public async Task<ReciboPagoDetalleApi?> ObtenerDetalleAsync(long idFichaPago, CancellationToken cancellationToken = default)
    {
        return await _api.GetAsync<ReciboPagoDetalleApi>($"{RutaRecibo}/{idFichaPago}", cancellationToken)
            .ConfigureAwait(false);
    }
}
