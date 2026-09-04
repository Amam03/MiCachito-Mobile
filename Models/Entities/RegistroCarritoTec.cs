namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Registro del carrito Tec (pantalla 14): CADA BILLETE ES UN REGISTRO
/// INDIVIDUAL (diff clave vs LOTENAL 11, que agrupa por tienda).
///
/// Todo se deriva del billete del catálogo (BilleteTec) y de su sorteo
/// (SorteoTec): no hay datos hardcoded. El precio individual es el
/// PRECIO DEL SORTEO tal cual (misma regla que la pantalla 11 pulida:
/// no se divide ni se inventa otra fuente).
/// </summary>
public class RegistroCarritoTec
{
    /// <summary>
    /// Billete de origen: al Eliminar se localiza la fila del catálogo
    /// por este id para quitar la pleca verde.
    /// </summary>
    public int IdBillete { get; init; }

    /// <summary>Número del billete (ej. "041042").</summary>
    public string NumeroBillete { get; init; } = string.Empty;

    /// <summary>Sorteo de origen (ej. "Sorteo Mi Sueño").</summary>
    public string SorteoTexto { get; init; } = string.Empty;

    /// <summary>
    /// Precio individual = precio del sorteo tal cual (SorteoTec.Precio).
    /// </summary>
    public decimal Precio { get; init; }

    /// <summary>Importe del registro (1 billete).</summary>
    public decimal Total => Precio;

    /// <summary>Color del sorteo (círculo de la tarjeta).</summary>
    public string ColorHex { get; init; } = string.Empty;

    /// <summary>Título de la tarjeta (ej. "Boleto 041042").</summary>
    public string BoletoTexto => $"Boleto {NumeroBillete}";

    /// <summary>Precio formateado (ej. "$700.00").</summary>
    public string PrecioTexto => $"${Precio:0.00}";

    /// <summary>Total formateado (ej. "Total: $700.00").</summary>
    public string TotalTexto => $"Total: ${Total:0.00}";
}
