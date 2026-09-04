namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Sorteo Tec mostrado en la pantalla "Seleccionar Sorteo" (pantalla 12).
///
/// Sorteos y precios verificados contra la BD del backend (tabla sorteos,
/// tipo_sorteo='sorteos_tec'): Educativo $400 (id 16), Dinero de X Vida
/// $490 (id 17), Mi Sueño $700 (id 18), Tradicional $1,350 (id 19).
/// El usuario definió mostrar SOLO el nombre del sorteo (sin No. ni fecha).
///
/// Los billetes son las MISMAS instancias compartidas del catálogo
/// (SorteosTecData): el estado Agregado muta aquí y se refleja en el
/// carrito (14) sin recargar.
/// </summary>
public class SorteoTec
{
    /// <summary>Id del sorteo. Backend: sorteos.id_sorteo (16-19).</summary>
    public int IdSorteo { get; init; }

    /// <summary>
    /// Nombre completo tal cual la BD (ej. "Sorteo Mi Sueño").
    /// Se usa como título de la pantalla 13 (mockup: "Sorteo Mi Sueño").
    /// </summary>
    public string NombreSorteo { get; init; } = string.Empty;

    /// <summary>
    /// Nombre corto: NombreSorteo sin el prefijo "Sorteo "
    /// (ej. "Mi Sueño", "Educativo").
    /// </summary>
    public string NombreCorto =>
        NombreSorteo.StartsWith("Sorteo ", StringComparison.OrdinalIgnoreCase)
            ? NombreSorteo["Sorteo ".Length..]
            : NombreSorteo;

    /// <summary>Precio del billete completo. Backend: precio_billete_completo.</summary>
    public decimal Precio { get; init; }

    /// <summary>Color de fondo de la tarjeta (pantalla 12).</summary>
    public string ColorHex { get; init; } = string.Empty;

    /// <summary>
    /// Billetes disponibles del sorteo (instancias compartidas del catálogo;
    /// su estado Agregado muta desde la pantalla 13).
    /// </summary>
    public IReadOnlyList<BilleteTec> Billetes { get; init; } = [];

    /// <summary>Línea 1 de la tarjeta (mockup 12: "Sorteos Tec").</summary>
    public string Linea1 => "Sorteos Tec";

    /// <summary>Línea 2 de la tarjeta: nombre corto en MAYÚSCULAS (mockup 12).</summary>
    public string Linea2 => NombreCorto.ToUpperInvariant();

    /// <summary>Precio formateado (ej. "$400", "$1,350").</summary>
    public string DisplayPrecio => $"${Precio:#,0}";
}
