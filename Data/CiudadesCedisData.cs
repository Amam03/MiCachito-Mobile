using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

/// <summary>
/// Catálogo mock de ciudades/CEDIS para la pantalla "Seleccionar Ciudad".
/// UI-only: los datos provienen del mockup 8_SrB_LOTENAL_seleccionar ciudad
/// (ciudades del sistema anterior Sr. Billetero).
///
/// REGLA DE NEGOCIO: la opción "CUALQUIER CIUDAD" debe ser SIEMPRE el
/// primer elemento de la lista; después aparecen las ciudades/CEDIS
/// correspondientes, en orden alfabético.
///
/// Futuro (integración): reemplazar por GET /api/cedis, que devuelve los
/// CEDIS con sede-scoping (corporativo = todos; regional = solo su CEDIS).
/// Ver NOTA de desincronización ciudad/estado en CiudadCedis.cs.
/// </summary>
public static class CiudadesCedisData
{
    /// <summary>
    /// Opción especial "Cualquier ciudad" (IdCiudad = 0, sin estado).
    /// Siempre debe ir primera en la lista.
    /// </summary>
    public static readonly CiudadCedis CualquierCiudad = new()
    {
        IdCiudad = 0,
        Nombre = "CUALQUIER CIUDAD",
        Estado = null,
    };

    /// <summary>
    /// Ciudades mock del mockup (orden alfabético, como en Sr. Billetero).
    /// Los IdCiudad son ficticios; en el backend corresponderán a
    /// cedis.id_cedis de los CEDIS donde se vende cada sorteo.
    /// </summary>
    private static readonly List<CiudadCedis> Ciudades = new()
    {
        new() { IdCiudad = 1, Nombre = "CARDENAS", Estado = "TABASCO" },
        new() { IdCiudad = 2, Nombre = "CHIHUAHUA", Estado = "CHIHUAHUA" },
        new() { IdCiudad = 3, Nombre = "COATZACOALCOS", Estado = "VERACRUZ" },
        new() { IdCiudad = 4, Nombre = "CORDOBA", Estado = "VERACRUZ" },
        new() { IdCiudad = 5, Nombre = "MINATITLAN", Estado = "VERACRUZ" },
        new() { IdCiudad = 6, Nombre = "OAXACA", Estado = "OAXACA" },
        new() { IdCiudad = 7, Nombre = "ORIZABA", Estado = "VERACRUZ" },
        new() { IdCiudad = 8, Nombre = "PIJIJIAPAN", Estado = "CHIAPAS" },
        new() { IdCiudad = 9, Nombre = "TAPACHULA", Estado = "CHIAPAS" },
    };

    /// <summary>
    /// Devuelve todas las opciones garantizando que "CUALQUIER CIUDAD"
    /// ocupe la posición 0, seguida de las ciudades ordenadas alfabéticamente.
    /// </summary>
    public static IReadOnlyList<CiudadCedis> ObtenerTodas()
    {
        var ciudades = new List<CiudadCedis> { CualquierCiudad };
        ciudades.AddRange(Ciudades.OrderBy(c => c.Nombre));
        return ciudades;
    }
}
