namespace MiCachito.Mobile.Helpers;

/// <summary>
/// Claves de almacenamiento (SecureStorage/Preferences).
/// Evita cadenas mágicas repartidas por el código.
/// </summary>
public static class StorageKeys
{
    /// <summary>
    /// Sesión serializada (SessionInfo) en SecureStorage.
    /// </summary>
    public const string Session = "session";

    /// <summary>
    /// Token de acceso Bearer.
    /// </summary>
    public const string AccessToken = "access_token";

    /// <summary>
    /// Token de refresco (por si el backend lo implementa en el futuro).
    /// </summary>
    public const string RefreshToken = "refresh_token";

    /// <summary>
    /// Datos del usuario autenticado.
    /// </summary>
    public const string Usuario = "usuario";

    /// <summary>
    /// Fecha/hora del último login.
    /// </summary>
    public const string LoginAt = "login_at";

    /// <summary>
    /// Fecha/hora de expiración del token.
    /// </summary>
    public const string ExpiresAt = "expires_at";

    /// <summary>
    /// Indica si el usuario pidió recordar la sesión.
    /// </summary>
    public const string RememberMe = "remember_me";
}
