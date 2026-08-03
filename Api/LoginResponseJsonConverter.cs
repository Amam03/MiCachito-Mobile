using System.Text.Json;
using System.Text.Json.Serialization;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;

namespace MiCachito.Mobile.Api;

/// <summary>
/// Convierte el objeto plano del login ({ ...usuario, "token": "..." }) en <see cref="LoginResponse"/>.
/// El backend no envía un "usuario" anidado, así que se deserializa el resto como <see cref="Usuario"/>
/// (los campos desconocidos, incluido "token", se ignoran) y se extrae el token por separado.
/// </summary>
public class LoginResponseJsonConverter : JsonConverter<LoginResponse>
{
    public override LoginResponse? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var usuario = root.Deserialize<Usuario>(options);

        string? token = null;
        if (root.TryGetProperty("token", out var tokenProperty) && tokenProperty.ValueKind == JsonValueKind.String)
        {
            token = tokenProperty.GetString();
        }

        return new LoginResponse { Usuario = usuario, Token = token };
    }

    public override void Write(Utf8JsonWriter writer, LoginResponse value, JsonSerializerOptions options)
        => throw new NotSupportedException("LoginResponse no se serializa hacia el backend.");
}
