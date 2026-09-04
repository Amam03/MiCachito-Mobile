namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Sorteo de Lotería Nacional (LOTENAL) mostrado en la pantalla Sr.B. LOTENAL.
/// </summary>
public class SorteoLotenal
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Primera línea del nombre (tipografía pequeña).
    /// </summary>
    public string Linea1 { get; init; } = string.Empty;

    /// <summary>
    /// Segunda línea del nombre (tipografía grande).
    /// </summary>
    public string Linea2 { get; init; } = string.Empty;

    public decimal Precio { get; init; }
    public string ColorHex { get; init; } = string.Empty;

    /// <summary>
    /// Subcódigo de 3 dígitos del barcode (140=Mayor, 261=Superior, etc.).
    /// Mappea a sorteos.subcodigo_sorteo en el backend.
    /// </summary>
    public string? SubcodigoSorteo { get; init; }

    public string DisplayPrecio => $"${Precio:0}";
}
