using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Servicio del flujo Tickets de Venta (Gestión, mockups 5, 5.1, 5.2) contra
/// la API real: historial (GET api/mobile/ventas) y detalle (GET
/// api/mobile/ventas/{id}) de las ventas del billetero de la sesión.
///
/// El id_billetero NUNCA se envía: el backend lo toma de la sesión y el
/// detalle de una venta ajena responde 404. Lanza ApiException en errores
/// HTTP; la red caída llega como HttpRequestException/TaskCanceled y los
/// VMs la capturan (patrón Depósitos).
///
/// ANTES: 3 movimientos TEMPORALES en memoria (fase solo-interfaz). La
/// fuente real ES este servicio: las ventas que registra POST
/// api/mobile/ventas/crear (LOTENAL y Sorteos Tec) son las que aparecen
/// aquí (ver docs/NOTAS_TICKETS_VENTA.md).
/// </summary>
public class TicketsVentaService
{
    private readonly IApiClient _api;

    public TicketsVentaService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>Historial de ventas del billetero (más reciente primero).</summary>
    public async Task<List<VentaHistorialApi>> CargarVentasAsync(CancellationToken cancellationToken = default)
    {
        List<VentaHistorialApi>? items = await _api
            .GetAsync<List<VentaHistorialApi>>(ApiEndpoints.MobileVentasHistorial.Historial, cancellationToken)
            .ConfigureAwait(false);
        return items ?? new List<VentaHistorialApi>();
    }

    /// <summary>
    /// Detalle de UNA venta del billetero de la sesión (404 si es ajena).
    /// </summary>
    /// <param name="idVenta">ventas.id_venta (clave de navegación).</param>
    public async Task<VentaDetalleApi> CargarDetalleAsync(int idVenta, CancellationToken cancellationToken = default)
    {
        VentaDetalleApi? detalle = await _api
            .GetAsync<VentaDetalleApi>(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, ApiEndpoints.MobileVentasHistorial.Detalle, idVenta),
                cancellationToken)
            .ConfigureAwait(false);

        return detalle ?? throw new ApiException(
            System.Net.HttpStatusCode.OK,
            "El servidor no devolvió el detalle de la venta");
    }
}
