namespace MiCachito.Mobile.Helpers;

/// <summary>
/// Configuración global de la aplicación.
/// Único punto donde se define la URL base del backend y valores de conexión.
/// </summary>
public static class AppSettings
{
    /// <summary>
    /// URL base del backend (Yii2 REST API).
    /// Cambiar aquí al desplegar a QA o Producción.
    /// </summary>
    public const string BaseUrl = "http://10.0.2.2:8080/";

    /// <summary>
    /// Timeout por defecto de las peticiones HTTP.
    /// </summary>
    public const int TimeoutSeconds = 30;

    /// <summary>
    /// Nombre de la aplicación.
    /// </summary>
    public const string AppName = "Mi Cachito";
}
