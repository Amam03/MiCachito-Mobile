namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Información resumida de la tienda del usuario (relación "tienda").
/// Solo presente si el usuario tiene una tienda asignada.
/// </summary>
public class TiendaInfo
{
    public int IdTienda { get; set; }

    public string? NombreTienda { get; set; }
}
