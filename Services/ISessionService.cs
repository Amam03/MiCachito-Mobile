using MiCachito.Mobile.Models;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Contrato de la sesión del usuario. La implementación usa SecureStorage.
/// Los ViewModels nunca tocan SecureStorage directamente; solo dependen de esta interfaz.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Sesión actual en memoria (null si no se ha cargado o no hay sesión).
    /// </summary>
    SessionInfo? CurrentSession { get; }

    /// <summary>
    /// Persiste una sesión a partir de la respuesta de login.
    /// </summary>
    Task SaveAsync(LoginResponse loginResponse, bool rememberMe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lee la sesión persistida y la cachea; null si no existe o no es válida.
    /// </summary>
    Task<SessionInfo?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza el usuario de la sesión (ej. tras auth/verify) y lo persiste.
    /// </summary>
    Task UpdateUsuarioAsync(Usuario usuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Borra la sesión persistida y la caché en memoria.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
