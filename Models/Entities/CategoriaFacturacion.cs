namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Categoría de sorteo del reporte de Facturación (agrupación de los
/// registros del periodo). El porcentaje se CALCULA sobre el total
/// facturado — nunca se escribe a mano. Color de presentación tomado
/// del mockup 9.3 (estilo, no dato).
/// </summary>
public class CategoriaFacturacion
{
    /// <summary>Nombre de la categoría (tipo de sorteo).</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Color del segmento en el pie y del indicador de la leyenda.</summary>
    public string ColorHex { get; set; } = string.Empty;

    /// <summary>Monto facturado de la categoría en el periodo (columna Venta).</summary>
    public decimal Monto { get; set; }

    /// <summary>Porcentaje sobre el total facturado (0-100, 1 decimal).</summary>
    public double Porcentaje { get; set; }
}
