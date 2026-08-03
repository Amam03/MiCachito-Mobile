using System.Text.Json;
using System.Text.Json.Serialization;
using MiCachito.Mobile.Models.Common;

namespace MiCachito.Mobile.Api;

/// <summary>
/// Normaliza el campo "errors" del backend.
/// El backend Yii2 puede enviarlo como:
///  - Objeto: { "campo": ["mensaje1", "mensaje2"] }
///  - Arreglo: ["mensaje1", "mensaje2"]
/// </summary>
public class ApiErrorConverter : JsonConverter<List<ApiError>>
{
    public override List<ApiError>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var result = new List<ApiError>();

        switch (root.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in root.EnumerateArray())
                {
                    result.Add(ReadItem(item));
                }
                break;

            case JsonValueKind.Object:
                foreach (var property in root.EnumerateObject())
                {
                    var field = property.Name;
                    var value = property.Value;

                    if (value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var message in value.EnumerateArray())
                        {
                            if (message.ValueKind == JsonValueKind.String)
                            {
                                result.Add(new ApiError { Field = field, Message = message.GetString() });
                            }
                        }
                    }
                    else if (value.ValueKind == JsonValueKind.String)
                    {
                        result.Add(new ApiError { Field = field, Message = value.GetString() });
                    }
                }
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                break;
        }

        return result.Count == 0 ? null : result;
    }

    public override void Write(Utf8JsonWriter writer, List<ApiError> value, JsonSerializerOptions options)
        => throw new NotSupportedException("Los errores no se serializan hacia el backend.");

    private static ApiError ReadItem(JsonElement item)
    {
        if (item.ValueKind == JsonValueKind.String)
        {
            return new ApiError { Message = item.GetString() };
        }

        if (item.ValueKind == JsonValueKind.Object)
        {
            string? field = null;
            string? message = null;

            foreach (var property in item.EnumerateObject())
            {
                if (property.Value.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var value = property.Value.GetString();
                if (string.Equals(property.Name, "field", StringComparison.OrdinalIgnoreCase))
                {
                    field = value;
                }
                else if (string.Equals(property.Name, "message", StringComparison.OrdinalIgnoreCase))
                {
                    message = value;
                }
                else if (message is null)
                {
                    message = value;
                }
            }

            return new ApiError { Field = field, Message = message };
        }

        return new ApiError { Message = item.ToString() };
    }
}
