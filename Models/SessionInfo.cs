using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Models;

/// <summary>
/// Sesión del expendio persistida en SecureStorage por SessionService.
/// Shape MOBILE (Fase 1): token de mobile_sesiones + expendio + billetero.
/// El token plano solo vive aquí y en memoria del backend jamás (allí va
/// hasheado); ExpiresAt llega como string SQL del campo "expira" del login.
/// </summary>
public class SessionInfo
{
    public string? AccessToken { get; set; }

    public ExpendioInfo? Expendio { get; set; }

    public BilleteroMobile? Billetero { get; set; }

    /// <summary>Id de la sesión en mobile_sesiones (dispositivo actual).</summary>
    public int IdSesion { get; set; }

    /// <summary>Etiqueta del dispositivo enviada en el login.</summary>
    public string? Dispositivo { get; set; }

    public DateTime? LoginAt { get; set; }

    /// <summary>Expiración de la sesión (string SQL "yyyy-MM-dd HH:mm:ss").</summary>
    public string? ExpiresAt { get; set; }

    public bool RememberMe { get; set; }
}
