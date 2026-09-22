namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Registro de facturación de un sorteo dentro del periodo (columnas
/// del PDF: Fecha, Sorteo, Entrega, Devolución, Venta, Ganancia).
/// FASE 3: datos reales de GET api/mobile/reportes/facturacion — una
/// fila por (fecha, sorteo); la CATEGORÍA (producto) agrupa el pie, la
/// leyenda y las secciones del PDF, y el SORTEO es la etiqueta de la fila.
/// </summary>
public class RegistroFacturacion
{
    /// <summary>Fecha del sorteo.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Etiqueta del sorteo (número o nombre, p. ej. "041753").</summary>
    public string Sorteo { get; set; } = string.Empty;

    /// <summary>Categoría/producto del sorteo ("Mayor", "Zodiaco"...) — agrupa pie/leyenda/secciones.</summary>
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Importe entregado en consigna.</summary>
    public decimal Entrega { get; set; }

    /// <summary>Importe devuelto (no vendido).</summary>
    public decimal Devolucion { get; set; }

    /// <summary>Importe vendido (facturado).</summary>
    public decimal Venta { get; set; }

    /// <summary>Ganancia del sorteo.</summary>
    public decimal Ganancia { get; set; }
}
