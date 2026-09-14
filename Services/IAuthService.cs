using MiCachito.Mobile.Models.Requests;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Contrato de autenticación MOBILE (api/mobile/auth/*, Fase 1).
/// Autentica expendios contra billeteros_expendios con sesión en
/// mobile_sesiones (multi-dispositivo, revocación individual).
/// Lanza <see cref="Api.ApiException"/> si el backend devuelve error.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// POST api/mobile/auth/login. Devuelve token + expendio + billetero,
    /// o null si la respuesta no trae data.
    /// </summary>
    Task<MobileLoginResponse?> LoginAsync(string username, string password, string? dispositivo = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// GET api/mobile/auth/verify. Valida el token actual (via IAuthTokenProvider)
    /// y devuelve expendio + billetero + sesión.
    /// </summary>
    Task<MobileVerifyResponse?> VerifyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// POST api/mobile/auth/logout. Revoca SOLO la sesión del token con que
    /// se llama (los demás dispositivos del expendio siguen vivos).
    /// </summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// GET api/mobile/auth/sesiones. Dispositivos con sesión activa del
    /// expendio autenticado (pantalla Cuenta).
    /// </summary>
    Task<List<SesionDispositivo>?> SesionesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// POST api/mobile/auth/sesiones/revocar con { id_sesion }.
    /// Solo admite sesiones del propio expendio (404 si es ajena).
    /// </summary>
    Task RevocarSesionAsync(int idSesion, CancellationToken cancellationToken = default);
}
