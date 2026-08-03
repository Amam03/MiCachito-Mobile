namespace MiCachito.Mobile.Models.Common;

/// <summary>
/// Envoltorio de respuesta del backend Yii2.
/// Éxito: {"success":true,"data":{...}} (message opcional).
/// Error:  {"success":false,"error":"..."} (errors array/dict solo en validaciones 422).
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public string? Error { get; set; }

    public List<ApiError>? Errors { get; set; }

    public T? Data { get; set; }
}
