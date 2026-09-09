namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Fila de movimiento del Fondo de Ahorro (tabla del PDF: Fecha,
/// Folio, Origen, Monto). Plantilla para los datos reales del
/// backend — en esta fase no se crean registros.
/// </summary>
public class MovimientoFondoAhorro
{
    /// <summary>Fecha del movimiento.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Folio de la operación.</summary>
    public string Folio { get; set; } = string.Empty;

    /// <summary>Origen del movimiento (depósito/retiro).</summary>
    public string Origen { get; set; } = string.Empty;

    /// <summary>Monto del movimiento.</summary>
    public decimal Monto { get; set; }
}
