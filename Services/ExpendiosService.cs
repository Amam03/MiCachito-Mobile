using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Implementa el contrato de Expendios mobile sobre <see cref="IApiClient"/>
/// (patrón de AuthService: sin estado, sin navegación).
/// Permisos compartidos a nivel billetero: PUT válida que el expendio
/// pertenezca al propio conjunto; el backend fija el UPDATE al billetero
/// de la sesión (nunca del request).
/// </summary>
public class ExpendiosService : IExpendiosService
{
    private readonly IApiClient _apiClient;

    public ExpendiosService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<MobileExpendiosResponse?> ObtenerAsync(CancellationToken cancellationToken = default)
        => _apiClient.GetAsync<MobileExpendiosResponse>(ApiEndpoints.MobileExpendios.Expendios, cancellationToken);

    public Task<MobilePermisosResponse?> ActualizarPermisosAsync(
        int idExpendio,
        int tieneProdDigitales,
        int tieneTiempoAire,
        int tieneLotenal,
        CancellationToken cancellationToken = default)
        => _apiClient.PutAsync<MobilePermisosResponse>(
            string.Format(ApiEndpoints.MobileExpendios.Permisos, idExpendio),
            new { tiene_prod_digitales = tieneProdDigitales, tiene_tiempo_aire = tieneTiempoAire, tiene_lotenal = tieneLotenal },
            cancellationToken);
}
