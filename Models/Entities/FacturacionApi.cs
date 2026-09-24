using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Respuesta de GET api/mobile/reportes/facturacion?fecha_inicio&amp;fecha_fin
/// (mockup 9.3): facturación histórica del billetero de la sesión en
/// DINERO, una fila por (fecha, sorteo). Venta y ganancia ya vienen
/// calculadas por el backend (ganancia = venta × comisión real); la app
/// NO recalcula lógica de negocio.
/// </summary>
public sealed class FacturacionApi
{
    /// <summary>Nombre del vendedor (billetero de la sesión).</summary>
    [JsonPropertyName("vendedor")]
    public string Vendedor { get; set; } = string.Empty;

    /// <summary>Porcentaje de comisión del billetero (ej. 10).</summary>
    [JsonPropertyName("comision_pct")]
    public decimal ComisionPct { get; set; }

    /// <summary>Registros del periodo, una fila por (fecha, sorteo).</summary>
    [JsonPropertyName("registros")]
    public List<RegistroFacturacionApi>? Registros { get; set; }
}

/// <summary>
/// Fila de facturación del periodo. Los montos (entrega, devolución,
/// venta, ganancia) llegan en DINERO redondeados a 2 decimales; sorteo
/// es la ETIQUETA de la fila y categoria el PRODUCTO que agrupa el pie
/// y las secciones del PDF.
/// </summary>
public sealed class RegistroFacturacionApi
{
    /// <summary>Fecha cruda del backend ("yyyy-MM-dd" o datetime de MySQL).</summary>
    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    /// <summary>Etiqueta del sorteo (numero_sorteo o nombre, p. ej. "041753").</summary>
    [JsonPropertyName("sorteo")]
    public string Sorteo { get; set; } = string.Empty;

    /// <summary>Categoría (producto real sin prefijo: Mayor, Zodiaco, Mi Sueño...).</summary>
    [JsonPropertyName("categoria")]
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Importe entregado en consigna (dinero).</summary>
    [JsonPropertyName("entrega")]
    public decimal Entrega { get; set; }

    /// <summary>Importe devuelto (no vendido).</summary>
    [JsonPropertyName("devolucion")]
    public decimal Devolucion { get; set; }

    /// <summary>Importe vendido (entrega − devolución).</summary>
    [JsonPropertyName("venta")]
    public decimal Venta { get; set; }

    /// <summary>Ganancia del sorteo (venta × comisión).</summary>
    [JsonPropertyName("ganancia")]
    public decimal Ganancia { get; set; }
}
