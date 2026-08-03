namespace MiCachito.Mobile.Models.Common;

/// <summary>
/// Representa un error de validación del backend dentro del campo "errors".
/// El backend Yii2 envía errores como diccionario { campo: ["mensaje"] } o arreglo de strings;
/// <see cref="ApiErrorConverter"/> normaliza ambos formatos.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Atributo del modelo que falló (ej. "username"); null si el error no es de un campo.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Mensaje de error legible.
    /// </summary>
    public string? Message { get; set; }

    public override string ToString()
        => string.IsNullOrWhiteSpace(Field)
            ? Message ?? string.Empty
            : $"{Field}: {Message}";
}
