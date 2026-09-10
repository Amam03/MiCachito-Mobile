namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Registro de facturación de un sorteo dentro del periodo (columnas
/// del PDF: Fecha, Sorteo, Entrega, Devolución, Venta, Ganancia).
/// Plantilla para los datos reales del backend.
/// </summary>
public class RegistroFacturacion
{
    /// <summary>Fecha del sorteo.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Categoría/tipo de sorteo (ej. "Mayor", "Superior").</summary>
    public string Sorteo { get; set; } = string.Empty;

    /// <summary>Importe entregado en consigna.</summary>
    public decimal Entrega { get; set; }

    /// <summary>Importe devuelto (no vendido).</summary>
    public decimal Devolucion { get; set; }

    /// <summary>Importe vendido (facturado).</summary>
    public decimal Venta { get; set; }

    /// <summary>Ganancia del sorteo.</summary>
    public decimal Ganancia { get; set; }
}
