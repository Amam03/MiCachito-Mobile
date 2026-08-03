namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Rol del usuario (relación "roles"). Formato: getApiData() del backend.
/// </summary>
public class RolInfo
{
    public int IdRol { get; set; }

    public string? NombreRol { get; set; }

    public string? Descripcion { get; set; }

    public string? Estatus { get; set; }

    /// <summary>
    /// Fecha de creación como texto (el backend la envía en formato SQL "yyyy-MM-dd HH:mm:ss",
    /// que System.Text.Json no convierte a DateTime sin un converter dedicado).
    /// </summary>
    public string? FechaCreacion { get; set; }
}
