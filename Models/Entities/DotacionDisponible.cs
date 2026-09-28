using System.Globalization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Dotación de Lotería Nacional con material LIVE asignado al billetero
/// de la sesión (GET api/mobile/ventas/sorteos, F1/F2). UNA fila por
/// (id_sorteo, numero_sorteo de billetes_loteria): dos dotaciones del
/// mismo sorteo son DOS filas (MAYOR - 4024 / MAYOR - 4025) y sus
/// billetes NO se mezclan (la agrupación backend es por dotación).
///
/// La fuente de verdad del estado LIVE es billetes_loteria vía
/// backend; esta entidad es solo la fila de la pantalla 7.x
/// (SorteosActivos). Sin regla artificial de fechas (§A.5): si el
/// backend la lista, está disponible.
///
/// Linea1/Linea2/ColorHex se mapean del catálogo de productos de la
/// pantalla 6 (SorteosLotenalData) por nombre de producto, para
/// conservar el diseño existente (catálogo real: pendiente v2 #9).
/// </summary>
public class DotacionDisponible
{
    /// <summary>Id del sorteo (PK sorteos).</summary>
    public int IdSorteo { get; init; }

    /// <summary>
    /// Número de dotación/recepción ("4024"); null = material legacy
    /// sin número (fila "sin número", misma convención que Gestión).
    /// </summary>
    public string? NumeroSorteo { get; init; }

    /// <summary>Nombre del producto LN en MAYÚSCULAS sin prefijo ("MAYOR").</summary>
    public string NombreProducto { get; init; } = string.Empty;

    /// <summary>
    /// Precio por cachito (fracción) del backend (sorteos.precio_fraccion).
    /// </summary>
    public decimal PrecioFraccion { get; init; }

    /// <summary>Fracciones libres totales de la dotación.</summary>
    public int FraccionesDisponibles { get; init; }

    /// <summary>Fecha de celebración (null = sin fecha).</summary>
    public DateOnly? FechaCelebracion { get; init; }

    // ── Presentación (pantalla 7.x: mismo diseño del mock) ──

    /// <summary>Primera línea del nombre (tipografía pequeña).</summary>
    public string Linea1 { get; init; } = "SORTEO";

    /// <summary>Segunda línea del nombre (tipografía grande).</summary>
    public string Linea2 { get; init; } = string.Empty;

    /// <summary>Color del tipo de sorteo (catálogo pantalla 6).</summary>
    public string ColorHex { get; init; } = "#4125F4";

    /// <summary>Precio corto ("$70").</summary>
    public string DisplayPrecio => $"${PrecioFraccion:0}";

    /// <summary>"No. 4024"; vacío en fila sin número.</summary>
    public string DisplayNumero => NumeroSorteo is null
        ? string.Empty
        : $"No. {NumeroSorteo}";

    /// <summary>Fecha corta es-MX ("24 sep"); vacía sin fecha.</summary>
    public string FechaSorteoTexto => FechaCelebracion is null
        ? string.Empty
        : FechaCelebracion.Value.ToString("dd MMM", new CultureInfo("es-MX"))
            .Replace(".", string.Empty)
            .ToLowerInvariant();
}

/// <summary>
/// Respuesta mapeada de GET api/mobile/ventas/billetes: encabezado de
/// UNA dotación + sus series con fracciones libres.
/// </summary>
public class DetalleDotacion
{
    /// <summary>Encabezado de la dotación consultada.</summary>
    public required DotacionDisponible Dotacion { get; init; }

    /// <summary>Series con fracciones libres (fila de la pantalla 9.x).</summary>
    public IReadOnlyList<SerieDisponible> Series { get; init; } = [];

    /// <summary>Total de fracciones libres de la dotación.</summary>
    public int TotalFracciones { get; init; }
}
