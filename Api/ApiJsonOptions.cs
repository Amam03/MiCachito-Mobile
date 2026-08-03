using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Api;

/// <summary>
/// Opciones de serialización compartidas entre el ApiClient y los convertidores.
/// Snake case (id_usuario, tipo_usuario) coincide con la nomenclatura del backend Yii2.
/// </summary>
internal static class ApiJsonOptions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new ApiErrorConverter(), new LoginResponseJsonConverter() },
    };

    public static JsonSerializerOptions Default => Options;
}
