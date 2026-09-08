using System.Globalization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Sorteo de Lotería Nacional con su fecha de celebración.
/// Se muestra en la pantalla de sorteos activos tras seleccionar un tipo.
/// Campos alineados con el modelo backend Sorteos.php.
/// </summary>
public class SorteoActivoLotenal
{
    public int IdSorteo { get; init; }

    /// <summary>
    /// Número visible del sorteo (ej. "4024", "2894").
    /// Mappea a sorteos.numero_sorteo en el backend.
    /// </summary>
    public string NumeroSorteo { get; init; } = string.Empty;

    /// <summary>
    /// Nombre completo del tipo de sorteo (ej. "SORTEO MAYOR").
    /// </summary>
    public string NombreSorteo { get; init; } = string.Empty;

    /// <summary>
    /// Primera línea del nombre (tipografía pequeña).
    /// </summary>
    public string Linea1 { get; init; } = string.Empty;

    /// <summary>
    /// Segunda línea del nombre (tipografía grande).
    /// </summary>
    public string Linea2 { get; init; } = string.Empty;

    /// <summary>
    /// Precio del billete completo. Mappea a precio_billete_completo.
    /// </summary>
    public decimal Precio { get; init; }

    /// <summary>
    /// Fecha de celebración del sorteo.
    /// Mappea a sorteos.fecha_sorteo en el backend.
    /// Se usa DateOnly para comparación sin hora.
    /// </summary>
    public DateOnly FechaCelebracion { get; init; }

    /// <summary>
    /// Color de fondo del botón (mismo color que el tipo en pantalla 6).
    /// </summary>
    public string ColorHex { get; init; } = string.Empty;

    /// <summary>
    /// Determina si el sorteo está disponible para venta.
    /// Regla: la fecha de celebración NO ha pasado (>= hoy).
    /// El backend además valida vigente=1 AND celebrado=0 (scope pendientes()).
    /// Por ahora solo aplicamos la regla de fecha.
    /// </summary>
    public bool EstaDisponible => FechaCelebracion >= DateOnly.FromDateTime(DateTime.Today);

    /// <summary>
    /// Precio formateado para display (ej. "$30").
    /// </summary>
    public string DisplayPrecio => $"${Precio:0}";

    /// <summary>
    /// Número formateado para display (ej. "No. 4024").
    /// </summary>
    public string DisplayNumero => $"No. {NumeroSorteo}";

    /// <summary>
    /// Fecha formateada en español corto (ej. "25 ago", "01 sep").
    /// </summary>
    public string FechaSorteoTexto =>
        FechaCelebracion.ToString("dd MMM", new CultureInfo("es-MX"))
            .Replace(".", string.Empty)
            .ToLowerInvariant();

    /// <summary>
    /// Nombre + número para listados compactos (ej. "MAYOR 4024").
    /// </summary>
    public string NombreCorto => $"{Linea2} {NumeroSorteo}";

    /// <summary>
    /// Fecha larga en español (ej. "08-septiembre-2026") para el selector
    /// de la pantalla Sorteos de Gestión.
    /// </summary>
    public string FechaLarga =>
        FechaCelebracion.ToString("dd-MMMM-yyyy", CultureInfo.CreateSpecificCulture("es-MX"));
}
