using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Requests;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Implementa el contrato de autenticación MOBILE sobre <see cref="IApiClient"/>.
/// No maneja almacenamiento de sesión ni navegación; eso corresponde a
/// SessionService/ViewModel.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;

    public AuthService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<MobileLoginResponse?> LoginAsync(string username, string password, string? dispositivo = null, CancellationToken cancellationToken = default)
        => _apiClient.PostAsync<MobileLoginResponse>(
            ApiEndpoints.MobileAuth.Login,
            new MobileLoginRequest(username, password, dispositivo),
            cancellationToken);

    public Task<MobileVerifyResponse?> VerifyAsync(CancellationToken cancellationToken = default)
        => _apiClient.GetAsync<MobileVerifyResponse>(ApiEndpoints.MobileAuth.Verify, cancellationToken);

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<object>(ApiEndpoints.MobileAuth.Logout, null, cancellationToken).ConfigureAwait(false);

    public Task<List<SesionDispositivo>?> SesionesAsync(CancellationToken cancellationToken = default)
        => _apiClient.GetAsync<List<SesionDispositivo>>(ApiEndpoints.MobileAuth.Sesiones, cancellationToken);

    public async Task RevocarSesionAsync(int idSesion, CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<object>(
            ApiEndpoints.MobileAuth.RevocarSesion,
            new { id_sesion = idSesion },
            cancellationToken).ConfigureAwait(false);
}
