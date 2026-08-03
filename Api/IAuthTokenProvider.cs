namespace MiCachito.Mobile.Api;

/// <summary>
/// Provee el token Bearer para inyectar el header Authorization.
/// Lo implementa SessionService (lee SecureStorage), pero ApiClient no conoce esa dependencia.
/// </summary>
public interface IAuthTokenProvider
{
    /// <summary>
    /// Devuelve el token de acceso actual, o null si no hay sesión.
    /// </summary>
    Task<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}
