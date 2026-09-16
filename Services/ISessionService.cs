using MiCachito.Mobile.Models;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Contrato de la sesión del expendio. La implementación usa SecureStorage.
/// Los ViewModels nunca tocan SecureStorage directamente; solo dependen de
/// esta interfaz.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Sesión actual en memoria (null si no se ha cargado o no hay sesión).
    /// </summary>
    SessionInfo? CurrentSession { get; }

    /// <summary>
    /// Persiste una sesión a partir de la respuesta del login mobile.
    /// </summary>
    Task SaveAsync(MobileLoginResponse loginResponse, string? dispositivo, bool rememberMe, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lee la sesión persistida y la cachea; null si no existe o no es válida.
    /// </summary>
    Task<SessionInfo?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza expendio/billetero/sesión de la sesión persistida
    /// (ej. tras api/mobile/auth/verify) y la persiste.
    /// </summary>
    Task UpdateAsync(MobileVerifyResponse verifyResponse, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza SOLO el billetero de la sesión persistida (ej. tras
    /// consultar/guardar Permisos de Venta) y la persiste. La pestaña
    /// Vender recarga estos flags en cada OnAppearing.
    /// </summary>
    Task UpdateBilleteroAsync(BilleteroMobile billetero, CancellationToken cancellationToken = default);

    /// <summary>
    /// Borra la sesión persistida y la caché en memoria.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
}
