using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Contrato de autenticación. Envuelve los endpoints públicos de auth:
/// login, logout, verify y refresh.
/// Lanza <see cref="Api.ApiException"/> si el backend devuelve error.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// POST auth/login. Devuelve usuario + token, o null si la respuesta no trae data.
    /// </summary>
    Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// GET auth/verify. Valida el token actual (via IAuthTokenProvider) y devuelve el usuario.
    /// </summary>
    Task<Usuario?> VerifyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// POST auth/logout. Invalida el token en el servidor.
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// POST auth/refresh. Devuelve el nuevo token, o null.
    /// </summary>
    Task<string?> RefreshTokenAsync(CancellationToken cancellationToken = default);
}
