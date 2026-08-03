namespace MiCachito.Mobile.Api;

/// <summary>
/// Contrato del cliente HTTP contra el backend.
/// Devuelve el contenido de "data" deserializado; lanza <see cref="ApiException"/>
/// en errores HTTP (no 2xx) o cuando el backend responde success=false.
/// Abstraído en interfaz para facilitar pruebas unitarias.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Ejecuta una petición GET y deserializa el campo "data" de la respuesta.
    /// </summary>
    Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta una petición POST con cuerpo JSON opcional y deserializa el campo "data".
    /// </summary>
    Task<T?> PostAsync<T>(string path, object? body = null, CancellationToken cancellationToken = default);
}
