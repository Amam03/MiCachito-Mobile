namespace MiCachito.Mobile.Helpers;

/// <summary>
/// Configuración global de la aplicación.
/// Único punto donde se define la URL base del backend y valores de conexión.
/// </summary>
public static class AppSettings
{
    /// <summary>
    /// URL base del backend (Yii2 REST API).
    ///
    /// Por defecto usa http://10.0.2.2:8080/ que es el alias estándar del
    /// emulador de Android para alcanzar el localhost de la máquina host.
    /// Funciona en cualquier computadora sin modificar el código.
    ///
    /// El backend puede ejecutarse de dos formas (siempre que quede
    /// disponible en el puerto 8080 del host):
    ///   A) Docker:     docker-compose up -d  en la carpeta del backend
    ///   B) PHP local:  php -S 0.0.0.0:8080 -t web  en la carpeta del backend
    ///
    /// Se puede sobreescribir con la variable de entorno API_BASE_URL:
    ///   export API_BASE_URL="http://servidor.example:8080/"
    ///   dotnet build -f net10.0-android -t:Run
    ///
    /// Casos:
    ///   - Emulador Android + backend local ............ http://10.0.2.2:8080/
    ///   - Dispositivo físico + backend en LAN ......... http://IP-LAN:8080/
    ///   - Windows/macOS (debug local) ................ http://localhost:8080/
    ///   - Backend remoto (QA/Producción) .............. http://servidor:8080/
    /// </summary>
    public static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://10.0.2.2:8080/";

    /// <summary>
    /// Timeout por defecto de las peticiones HTTP.
    /// </summary>
    public const int TimeoutSeconds = 30;

    /// <summary>
    /// Nombre de la aplicación.
    /// </summary>
    public const string AppName = "Mi Cachito";
}