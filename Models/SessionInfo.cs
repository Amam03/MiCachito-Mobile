using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Models;

/// <summary>
/// Sesión del usuario persistida en SecureStorage por SessionService.
/// Incluye el token, el usuario y metadatos de la sesión (fechas, recordar sesión).
/// </summary>
public class SessionInfo
{
    public string? AccessToken { get; set; }

    /// <summary>
    /// Token de refresco (reservado para cuando el backend lo implemente).
    /// </summary>
    public string? RefreshToken { get; set; }

    public Usuario? Usuario { get; set; }

    public DateTime? LoginAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool RememberMe { get; set; }
}
