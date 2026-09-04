using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

/// <summary>
/// Catálogo mock de tiendas/boletos disponibles para la pantalla
/// "Agregar Boletos" (pantallas 9.1 / 9.2).
/// UI-only: los datos provienen de los mockups
/// 9.1_SrB_LOTENAL_agregar boletos.png (sorteo normal) y
/// 9.2_SrB_LOTENAL_agregar boletos (zodiaco).png (zodiaco y
/// zodiaco especial), transcritos verbatim (números, fracciones,
/// signos, ciudades y estados).
///
/// REGLA DE FILTRO POR CIUDAD (pantalla 8):
///   - "Cualquier ciudad" (IdCiudad = 0) muestra TODAS las filas.
///   - Una ciudad específica muestra solo las filas de esa ciudad.
///
/// NOTA: los IdCiudad 1-9 corresponden al catálogo de la pantalla 8
/// (CiudadesCedisData). Las ciudades que aparecen en los mockups 9.x
/// pero NO están en el catálogo de la pantalla 8 (TUXTLA GUTIERREZ,
/// TEHUACAN, VILLAHERMOSA) usan Ids 10-12: solo se muestran en modo
/// "Cualquier ciudad" (desde la pantalla 8 no se pueden seleccionar).
///
/// Futuro (integración): reemplazar por el endpoint de disponibilidad
/// de boletos por sorteo/ciudad (ver NOTAS_NEGOCIO_SORTEOS.md).
/// </summary>
public static class TiendasDisponiblesData
{
    /// <summary>
    /// Filas para sorteos normales (mockup 9.1):
    /// línea 1 = número + fracciones, línea 2 = ciudad, estado.
    /// Datos verbatim del mockup (3 paneles = 9 filas).
    /// </summary>
    private static readonly List<TiendaDisponible> TiendasNormales = new()
    {
        new() { IdTienda = 1, Numero = "2168",  FraccionesTexto = "0/20", Signo = null, Ciudad = "TUXTLA GUTIERREZ", Estado = "CHIAPAS",   IdCiudad = 10 },
        new() { IdTienda = 2, Numero = "3768",  FraccionesTexto = "0/20", Signo = null, Ciudad = "CHIHUAHUA",        Estado = "CHIHUAHUA", IdCiudad = 2 },
        new() { IdTienda = 3, Numero = "4938",  FraccionesTexto = "0/19", Signo = null, Ciudad = "TAPACHULA",        Estado = "CHIAPAS",   IdCiudad = 9 },
        new() { IdTienda = 4, Numero = "19004", FraccionesTexto = "0/20", Signo = null, Ciudad = "CHIHUAHUA",        Estado = "CHIHUAHUA", IdCiudad = 2 },
        new() { IdTienda = 5, Numero = "22424", FraccionesTexto = "0/17", Signo = null, Ciudad = "TUXTLA GUTIERREZ", Estado = "CHIAPAS",   IdCiudad = 10 },
        new() { IdTienda = 6, Numero = "22794", FraccionesTexto = "0/19", Signo = null, Ciudad = "TAPACHULA",        Estado = "CHIAPAS",   IdCiudad = 9 },
        new() { IdTienda = 7, Numero = "27069", FraccionesTexto = "0/20", Signo = null, Ciudad = "COATZACOALCOS",    Estado = "VERACRUZ",  IdCiudad = 3 },
        new() { IdTienda = 8, Numero = "39269", FraccionesTexto = "0/20", Signo = null, Ciudad = "TEHUACAN",         Estado = "PUEBLA",    IdCiudad = 11 },
        new() { IdTienda = 9, Numero = "45449", FraccionesTexto = "0/20", Signo = null, Ciudad = "CORDOBA",          Estado = "VERACRUZ",  IdCiudad = 4 },
    };

    /// <summary>
    /// Filas para sorteos Zodiaco y Zodiaco Especial (mockups 9.2):
    /// línea 1 = número + fracciones, línea 2 = SIGNO,
    /// línea 3 = ciudad, estado.
    /// Unión de las capturas 9.2 (zodiaco/zodiaco especial y su vista
    /// filtrada por Aries), transcritas verbatim.
    /// </summary>
    private static readonly List<TiendaDisponible> TiendasZodiaco = new()
    {
        new() { IdTienda = 1, Numero = "739",  FraccionesTexto = "0/20", Signo = "CANCER",   Ciudad = "VILLAHERMOSA",     Estado = "TABASCO",  IdCiudad = 12 },
        new() { IdTienda = 2, Numero = "1861", FraccionesTexto = "0/20", Signo = "ARIES",    Ciudad = "VILLAHERMOSA",     Estado = "TABASCO",  IdCiudad = 12 },
        new() { IdTienda = 3, Numero = "2019", FraccionesTexto = "0/20", Signo = "VIRGO",    Ciudad = "TUXTLA GUTIERREZ", Estado = "CHIAPAS",  IdCiudad = 10 },
        new() { IdTienda = 4, Numero = "2849", FraccionesTexto = "0/20", Signo = "PISCIS",   Ciudad = "CORDOBA",          Estado = "VERACRUZ", IdCiudad = 4 },
        new() { IdTienda = 5, Numero = "2971", FraccionesTexto = "0/20", Signo = "ARIES",    Ciudad = "CORDOBA",          Estado = "VERACRUZ", IdCiudad = 4 },
        new() { IdTienda = 6, Numero = "4208", FraccionesTexto = "0/19", Signo = "ARIES",    Ciudad = "CORDOBA",          Estado = "VERACRUZ", IdCiudad = 4 },
        new() { IdTienda = 7, Numero = "4918", FraccionesTexto = "0/20", Signo = "ESCORPION", Ciudad = "TUXTLA GUTIERREZ", Estado = "CHIAPAS",  IdCiudad = 10 },
        new() { IdTienda = 8, Numero = "5568", FraccionesTexto = "0/19", Signo = "GEMINIS",  Ciudad = "CORDOBA",          Estado = "VERACRUZ", IdCiudad = 4 },
        new() { IdTienda = 9, Numero = "6151", FraccionesTexto = "0/20", Signo = "ARIES",    Ciudad = "CARDENAS",         Estado = "TABASCO",  IdCiudad = 1 },
    };

    /// <summary>
    /// Devuelve las tiendas disponibles ordenadas por número de billete
    /// (ascendente), filtradas por ciudad:
    ///   - ciudadId = 0 ("Cualquier ciudad") → todas las filas.
    ///   - ciudadId &gt; 0 → solo las filas de esa ciudad.
    /// esZodiaco selecciona el catálogo zodiaco (tipos 3 y 4) o el normal.
    /// </summary>
    public static IReadOnlyList<TiendaDisponible> ObtenerPorCiudad(int ciudadId, bool esZodiaco)
    {
        IEnumerable<TiendaDisponible> fuente = esZodiaco ? TiendasZodiaco : TiendasNormales;

        if (ciudadId > 0)
        {
            fuente = fuente.Where(t => t.IdCiudad == ciudadId);
        }

        // Orden del mockup: ascendente por VALOR numérico del billete
        // (no lexicográfico: "739" debe ir antes que "1861").
        return fuente
            .OrderBy(t => int.TryParse(t.Numero, out var n) ? n : int.MaxValue)
            .ToList();
    }
}
