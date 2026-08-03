using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Requests;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Implementa el contrato de autenticación sobre <see cref="IApiClient"/>.
/// No maneja almacenamiento de sesión ni navegación; eso corresponde a SessionService/ViewModel.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;

    public AuthService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        => _apiClient.PostAsync<LoginResponse>(
            ApiEndpoints.Auth.Login,
            new LoginRequest(username, password),
            cancellationToken);

    public Task<Usuario?> VerifyAsync(CancellationToken cancellationToken = default)
        => _apiClient.GetAsync<Usuario>(ApiEndpoints.Auth.Verify, cancellationToken);

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
        => await _apiClient.PostAsync<object>(ApiEndpoints.Auth.Logout, null, cancellationToken).ConfigureAwait(false);

    public Task<string?> RefreshTokenAsync(CancellationToken cancellationToken = default)
        => _apiClient.PostAsync<string>(ApiEndpoints.Auth.Refresh, null, cancellationToken);
}
