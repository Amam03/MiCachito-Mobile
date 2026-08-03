using System.Net;
using MiCachito.Mobile.Models.Common;

namespace MiCachito.Mobile.Api;

/// <summary>
/// Excepción lanzada cuando el backend responde con un error.
/// HTTP no 2xx, o respuesta 2xx con success=false.
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Mensaje de error del backend (campo "error"), o null si no lo proporcionó.
    /// </summary>
    public string? ServerMessage { get; }

    /// <summary>
    /// Errores de validación (campo "errors"), normalizados por ApiErrorConverter.
    /// </summary>
    public IReadOnlyList<ApiError>? Errors { get; }

    public ApiException(
        HttpStatusCode statusCode,
        string? serverMessage = null,
        IReadOnlyList<ApiError>? errors = null,
        Exception? innerException = null)
        : base(serverMessage ?? $"La solicitud falló (HTTP {(int)statusCode}).", innerException)
    {
        StatusCode = statusCode;
        ServerMessage = serverMessage;
        Errors = errors;
    }
}
