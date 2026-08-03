namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Información resumida del CEDIS del usuario (relación "cedis").
/// Solo presente si el usuario tiene un CEDIS asignado.
/// </summary>
public class CedisInfo
{
    public int IdCedis { get; set; }

    public string? NombreCedis { get; set; }
}
