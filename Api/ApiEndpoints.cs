namespace MiCachito.Mobile.Api;

/// <summary>
/// Rutas relativas del backend (sin la URL base, que vive en AppSettings).
/// Se agrupan por dominio para crecer ordenadamente (Ventas, Premios, Devoluciones, Reportes...).
/// </summary>
public static class ApiEndpoints
{
    /// <summary>
    /// Autenticación MOBILE (Fase 1): expendios contra billeteros_expendios
    /// + mobile_sesiones. Únicos endpoints de auth que usa la app.
    /// Contrato en docs/backend-integracion.md §3.
    /// </summary>
    public static class MobileAuth
    {
        public const string Login = "api/mobile/auth/login";
        public const string Verify = "api/mobile/auth/verify";
        public const string Logout = "api/mobile/auth/logout";
        public const string Sesiones = "api/mobile/auth/sesiones";
        public const string RevocarSesion = "api/mobile/auth/sesiones/revocar";
    }
}