using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Models.Responses;

/// <summary>
/// Resultado de POST auth/login.
/// El backend responde con un objeto plano: todos los campos del usuario más "token".
/// <see cref="LoginResponseJsonConverter"/> separa el objeto plano en Usuario y Token.
/// </summary>
public class LoginResponse
{
    public Usuario? Usuario { get; set; }

    public string? Token { get; set; }
}
