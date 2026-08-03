namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Usuario autenticado. Coincide con getApiDataCompleto() del backend (login y verify).
/// "ultimo_acceso" y "fecha_creacion" se mantienen como texto porque el backend las envía
/// en formato SQL "yyyy-MM-dd HH:mm:ss", que System.Text.Json no convierte a DateTime.
/// </summary>
public class Usuario
{
    public int IdUsuario { get; set; }

    public string? Username { get; set; }

    public string? TipoUsuario { get; set; }

    public int? IdCedis { get; set; }

    public int? IdTienda { get; set; }

    public string? Estatus { get; set; }

    public string? UltimoAcceso { get; set; }

    public string? FechaCreacion { get; set; }

    public CedisInfo? Cedis { get; set; }

    public TiendaInfo? Tienda { get; set; }

    public List<RolInfo>? Roles { get; set; }

    /// <summary>
    /// Permisos como "modulo.accion" (ej. "ventas.crear").
    /// </summary>
    public List<string>? Permisos { get; set; }
}
