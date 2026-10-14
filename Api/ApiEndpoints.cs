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

    /// <summary>
    /// Expendios MOBILE: conjunto de expendios del billetero autenticado +
    /// Permisos de Venta compartidos a nivel billetero (2026-09-16).
    /// </summary>
    public static class MobileExpendios
    {
        /// <summary>GET: lista del conjunto + estado compartido de permisos.</summary>
        public const string Expendios = "api/mobile/expendios";

        /// <summary>PUT: actualizar permisos del propio billetero vía un
        /// expendio del conjunto. {0} = id_expendio.</summary>
        public const string Permisos = "api/mobile/expendios/{0}/permisos";
    }

    /// <summary>
    /// Tickets de Venta MOBILE (Gestión, mockups 5.x): historial y detalle
    /// de las VENTAS REALES del billetero de la sesión (misma tabla que
    /// POST ventas/crear). Scope siempre al id_billetero de la sesión.
    /// </summary>
    public static class MobileVentasHistorial
    {
        /// <summary>GET: historial de ventas (todas, cualquier estatus — D5).</summary>
        public const string Historial = "api/mobile/ventas";

        /// <summary>GET: detalle de una venta. {0} = id_venta.</summary>
        public const string Detalle = "api/mobile/ventas/{0}";
    }

    /// <summary>
    /// Premios y Reintegros MOBILE (Gestión, mockups 3.x): histórico y
    /// detalle de los boletos ACEPTADOS por escaneo (D1: sin "NO TIENE
    /// PREMIO"), folio servidor PYR-YYYYMMDD-NNNN (D3), detalle agrupado
    /// por sorteo (D2). Scope siempre al id_billetero de la sesión.
    /// </summary>
    public static class MobilePremiosReintegros
    {
        /// <summary>GET: histórico de movimientos (fecha descendente).</summary>
        public const string Historial = "api/mobile/premios-reintegros";

        /// <summary>POST: guardar la captura en curso (folio del backend).</summary>
        public const string Guardar = "api/mobile/premios-reintegros";

        /// <summary>GET: detalle agrupado por sorteo. {0} = id_movimiento.</summary>
        public const string Detalle = "api/mobile/premios-reintegros/{0}";
    }
}