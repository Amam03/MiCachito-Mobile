using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

/// <summary>
/// Cache EN MEMORIA de los sorteos y boletos TEC que devuelve la API.
///
/// SUSTITUYE a SorteosTecData (catálogo fijo con ids desfasados y un boleto
/// inventado por sorteo). Aquí no hay ningún dato hardcodeado: lo que se
/// guarda es exactamente lo que respondió
/// `api/mobile/ventas/sorteos-tec` y `.../billetes-tec`.
///
/// Existe porque el carrito (pantalla 14) necesita, para cada boleto del
/// carrito, el nombre y el PRECIO de su sorteo; y el boleto por sí solo no
/// los trae. La pantalla 12 y la 13 ya cargaron esos sorteos, así que se
/// reaprovechan en vez de volver a pedir lo mismo al backend.
///
/// Es una cache de proceso (se pierde al cerrar la app): al volver a la
/// pantalla 12 los datos se recargan de la API, que es la fuente de verdad.
/// </summary>
public static class SorteosTecCatalogo
{
    /// <summary>Sorteos TEC cargados desde la API, por id.</summary>
    private static readonly Dictionary<int, SorteoTec> Sorteos = new();

    /// <summary>Boletos TEC cargados desde la API, por id.</summary>
    private static readonly Dictionary<int, BilleteTec> Billetes = new();

    /// <summary>
    /// Registra los sorteos que devolvió la API (pantalla 12).
    /// </summary>
    public static void CargarSorteos(IEnumerable<SorteoTec> sorteos)
    {
        Sorteos.Clear();
        foreach (SorteoTec sorteo in sorteos)
        {
            Sorteos[sorteo.IdSorteo] = sorteo;
        }
    }

    /// <summary>
    /// Registra los boletos que devolvió la API (pantalla 13).
    /// </summary>
    public static void CargarBilletes(IEnumerable<BilleteTec> boletos)
    {
        foreach (BilleteTec billete in boletos)
        {
            Billetes[billete.IdBillete] = billete;
        }
    }

    /// <summary>Sorteo por id, o null si no se ha cargado.</summary>
    public static SorteoTec? ObtenerSorteo(int idSorteo) =>
        Sorteos.TryGetValue(idSorteo, out SorteoTec? sorteo) ? sorteo : null;

    /// <summary>
    /// Boletos del carrito, resueltos contra la cache. Los que no estén (por
    /// ejemplo, si la app se reinició a mitad del flujo) se omiten en vez de
    /// inventar un boleto: el carrito queda solo con lo que se conoce.
    /// </summary>
    public static List<BilleteTec> BilletesPorIds(IReadOnlyList<int> ids) =>
        ids.Where(id => Billetes.ContainsKey(id))
            .Select(id => Billetes[id])
            .ToList();

    /// <summary>Vacía la cache (al recargar la pantalla 12).</summary>
    public static void LimpiarBilletes() => Billetes.Clear();

    /// <summary>Vacía toda la cache.</summary>
    public static void LimpiarTodo()
    {
        Sorteos.Clear();
        Billetes.Clear();
    }
}

/// <summary>
/// Catálogo de sorteos Tec (pantalla 12).
///
/// Sorteos y precios VERIFICADOS contra la BD del backend (tabla sorteos,
/// tipo_sorteo='sorteos_tec', sep-2026):
///   id 16 TEC-EDU    "Sorteo Educativo"        $400
///   id 17 TEC-XVIDA  "Sorteo Dinero de X Vida" $490
///   id 18 TEC-SUENO  "Sorteo Mi Sueño"         $700
///   id 19 TEC-TRAD   "Sorteo Tradicional"      $1,350
///
/// Colores: ASIGNADOS por el usuario (sep-2026; el mockup 12 solo trae
/// 3 tarjetas y ninguna es Educativo/Dinero de X Vida):
///   Educativo NARANJA, Dinero de X Vida VERDE AZULADO,
///   Mi Sueño MORADO #7D44B7 (medido en mockup 12),
///   Tradicional AZUL #0097DC (medido en mockup 12).
///
/// BILLETES: por decisión del usuario (sep-2026) UN billete de PRUEBA
/// por sorteo, con números distintos entre sí (la BD solo tiene 5
/// billetes de prueba de Mi Sueño y 0 de los otros 3).
///
/// En producción se consultarán via SorteosTecController / billetes.
/// </summary>
