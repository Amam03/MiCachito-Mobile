namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Fila de movimiento del Fondo de Ahorro (tabla del PDF: Fecha,
/// Folio, Origen, Descripción, Monto). Llegan del endpoint real
/// GET api/mobile/reportes/fondo-ahorro: el backend entrega los
/// montos SIEMPRE positivos y la app deriva el signo del origen
/// (retiro → negativo) al mapear en FondoAhorroService.
/// </summary>
public class MovimientoFondoAhorro
{
    /// <summary>Fecha del movimiento.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Folio de la operación (id_fondo_ahorro).</summary>
    public string Folio { get; set; } = string.Empty;

    /// <summary>Origen del movimiento (aportacion | retiro).</summary>
    public string Origen { get; set; } = string.Empty;

    /// <summary>Monto con signo: positivo (aportación) / negativo (retiro).</summary>
    public decimal Monto { get; set; }

    /// <summary>Descripción (observaciones o etiqueta del backend).</summary>
    public string Descripcion { get; set; } = string.Empty;
}
