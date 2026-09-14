using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Models.Responses;

/// <summary>
/// Subobjeto "sesion" de GET api/mobile/auth/verify.
/// </summary>
public class SesionInfo
{
    public int IdSesion { get; set; }

    public string? Dispositivo { get; set; }

    /// <summary>Expiración (string SQL "yyyy-MM-dd HH:mm:ss").</summary>
    public string? FechaExpiracion { get; set; }
}

/// <summary>
/// Resultado de GET api/mobile/auth/verify (campo "data").
/// Dice quién está autenticado y con qué sesión (para restaurar al arranque).
/// </summary>
public class MobileVerifyResponse
{
    public ExpendioInfo? Expendio { get; set; }

    public BilleteroMobile? Billetero { get; set; }

    public SesionInfo? Sesion { get; set; }
}

/// <summary>
/// Elemento de GET api/mobile/auth/sesiones: dispositivo con sesión activa
/// del expendio (pantalla Cuenta / revocación individual).
/// </summary>
public class SesionDispositivo
{
    public int IdSesion { get; set; }

    public string? Dispositivo { get; set; }

    /// <summary>Alta de la sesión (string SQL "yyyy-MM-dd HH:mm:ss").</summary>
    public string? FechaCreacion { get; set; }

    public string? UltimoAcceso { get; set; }

    public string? FechaExpiracion { get; set; }

    /// <summary>True si es la sesión con la que se hizo la petición.</summary>
    public bool Actual { get; set; }
}
