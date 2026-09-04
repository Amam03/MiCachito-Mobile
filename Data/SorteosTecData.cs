using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

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
public static class SorteosTecData
{
    /// <summary>Todos los sorteos Tec (siempre las mismas instancias).</summary>
    public static IReadOnlyList<SorteoTec> ObtenerTodos() => _sorteos;

    /// <summary>Sorteo por id (navegación desde la pantalla 12).</summary>
    public static SorteoTec? ObtenerPorId(int idSorteo) =>
        _sorteos.FirstOrDefault(s => s.IdSorteo == idSorteo);

    /// <summary>
    /// Todos los billetes agregados al carrito (de cualquier sorteo):
    /// alimenta el carrito Tec (pantalla 14) y los badges.
    /// </summary>
    public static IEnumerable<BilleteTec> BilletesAgregados() =>
        _sorteos.SelectMany(s => s.Billetes).Where(b => b.Agregado);

    /// <summary>Número de billetes agregados (badge de pantallas 12/13).</summary>
    public static int TotalAgregados() =>
        _sorteos.Sum(s => s.Billetes.Count(b => b.Agregado));

    private static readonly List<SorteoTec> _sorteos =
    [
        new()
        {
            IdSorteo = 16,
            NombreSorteo = "Sorteo Educativo",
            Precio = 400m,
            // Naranja (asignado por el usuario)
            ColorHex = "#F57C00",
            Billetes =
            [
                new BilleteTec { IdBillete = 1601, IdSorteo = 16, Numero = "118425" }
            ]
        },
        new()
        {
            IdSorteo = 17,
            NombreSorteo = "Sorteo Dinero de X Vida",
            Precio = 490m,
            // Verde azulado (asignado por el usuario)
            ColorHex = "#00897B",
            Billetes =
            [
                new BilleteTec { IdBillete = 1701, IdSorteo = 17, Numero = "237914" }
            ]
        },
        new()
        {
            IdSorteo = 18,
            NombreSorteo = "Sorteo Mi Sueño",
            Precio = 700m,
            // Morado (medido en mockup 12)
            ColorHex = "#7D44B7",
            Billetes =
            [
                new BilleteTec { IdBillete = 1801, IdSorteo = 18, Numero = "041042" }
            ]
        },
        new()
        {
            IdSorteo = 19,
            NombreSorteo = "Sorteo Tradicional",
            Precio = 1350m,
            // Azul (medido en mockup 12)
            ColorHex = "#0097DC",
            Billetes =
            [
                new BilleteTec { IdBillete = 1901, IdSorteo = 19, Numero = "564738" }
            ]
        },
    ];
}
