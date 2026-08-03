namespace MiCachito.Mobile.Models.Requests;

/// <summary>
/// Cuerpo de la petición POST auth/login.
/// Se serializa en snake_case: { "username": ..., "password": ... }.
/// </summary>
public class LoginRequest
{
    public string? Username { get; set; }

    public string? Password { get; set; }

    public LoginRequest(string username, string password)
    {
        Username = username;
        Password = password;
    }
}
