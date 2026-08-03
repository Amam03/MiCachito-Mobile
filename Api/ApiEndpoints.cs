namespace MiCachito.Mobile.Api;

/// <summary>
/// Rutas relativas del backend (sin la URL base, que vive en AppSettings).
/// Se agrupan por dominio para crecer ordenadamente (Ventas, Premios, Devoluciones, Reportes...).
/// </summary>
public static class ApiEndpoints
{
    public static class Auth
    {
        public const string Login = "api/auth/login";
        public const string Logout = "api/auth/logout";
        public const string Verify = "api/auth/verify";
        public const string Refresh = "api/auth/refresh";
    }
}