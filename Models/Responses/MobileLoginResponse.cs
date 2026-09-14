using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Models.Responses;

/// <summary>
/// Resultado de POST api/mobile/auth/login (campo "data" del envelope).
/// Contrato mobile Fase 0: { token, expira, expendio, billetero }.
/// El token plano se entrega UNA sola vez aquí; el resto del tiempo viaja
/// como hash SHA-256 en mobile_sesiones.
/// </summary>
public class MobileLoginResponse
{
    public string? Token { get; set; }

    /// <summary>Expiración de la sesión (string SQL "yyyy-MM-dd HH:mm:ss").</summary>
    public string? Expira { get; set; }

    public ExpendioInfo? Expendio { get; set; }

    public BilleteroMobile? Billetero { get; set; }
}
