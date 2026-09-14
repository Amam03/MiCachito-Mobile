namespace MiCachito.Mobile.Models.Requests;

/// <summary>
/// Cuerpo de la petición POST api/mobile/auth/login.
/// Se serializa en snake_case: { "username": ..., "password": ..., "dispositivo": ... }.
/// "dispositivo" es opcional para el backend (etiqueta de la sesión para
/// la lista de dispositivos); la app envía DeviceInfo.Current.Name.
/// </summary>
public class MobileLoginRequest
{
    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Dispositivo { get; set; }

    public MobileLoginRequest(string username, string password, string? dispositivo = null)
    {
        Username = username;
        Password = password;
        Dispositivo = dispositivo;
    }
}
