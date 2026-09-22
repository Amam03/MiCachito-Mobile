using System.Collections.ObjectModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Reporte Estado de Cuenta (pestaña 1 de Reportes, mockups 9/9.1).
/// FASE 4 (2026-09-22): datos REALES de GET api/mobile/reportes/estado-cuenta.
/// Conceptos y totales llegan calculados por el backend (fórmulas
/// validadas en Fase 1); las filas de sorteos contienen SOLO
/// consignaciones vivas — las ventas solo alimentan Pagarés y
/// al_corriente. Los totales se toman del bloque "totales" del endpoint
/// (el backend es la fuente; la app NO recalcula sumas).
/// </summary>
public class EstadoCuenta
{
    /// <summary>Fecha de emisión del reporte (del endpoint).</summary>
    public DateTime FechaEmision { get; set; } = DateTime.Today;

    /// <summary>Nombre del vendedor (billetero de la sesión, para el PDF).</summary>
    public string Vendedor { get; set; } = string.Empty;

    /// <summary>Plaza/Zona (nombre del CEDIS, para el PDF).</summary>
    public string Plaza { get; set; } = string.Empty;

    /// <summary>
    /// True si ningún documento vencido conserva saldo real. FASE 4: se
    /// recibe del backend pero NO se muestra (decisión Fase 1: sin UI
    /// nueva; el mockup 9.1 no lo pinta).
    /// </summary>
    public bool AlCorriente { get; set; }

    /// <summary>Fondo de Ahorro del bloque de resumen.</summary>
    public decimal FondoDeAhorro { get; set; }

    /// <summary>Fideicomiso del bloque de resumen.</summary>
    public decimal Fideicomiso { get; set; }

    /// <summary>Pagarés del bloque de resumen.</summary>
    public decimal Pagares { get; set; }

    /// <summary>Bolsa Electrónica del bloque de resumen.</summary>
    public decimal BolsaElectronica { get; set; }

    /// <summary>Garantía Total del bloque de resumen.</summary>
    public decimal GarantiaTotal { get; set; }

    /// <summary>Capacidad de Crédito del bloque de resumen.</summary>
    public decimal CapacidadDeCredito { get; set; }

    /// <summary>
    /// Sorteos del estado de cuenta (SOLO consignaciones vivas agrupadas
    /// por sorteo, semántica Fase 1). Vacío = sin documentos.
    /// </summary>
    public ObservableCollection<SorteoEstadoCuenta> Sorteos { get; } = new();

    /// <summary>Total de la columna Vencido (del bloque totales del endpoint).</summary>
    public decimal VencidoTotal { get; set; }

    /// <summary>Total de la columna Consigna (del bloque totales del endpoint).</summary>
    public decimal ConsignaTotal { get; set; }

    /// <summary>Total de la columna Pagos (del bloque totales del endpoint).</summary>
    public decimal PagosTotal { get; set; }

    /// <summary>Total final = Consigna − Pagos (del endpoint; puede ser negativo).</summary>
    public decimal TotalFinal { get; set; }
}

/// <summary>
/// Fila de sorteo del Estado de Cuenta (tarjeta 9.1 y tabla del PDF:
/// Sorteo, Fecha, Cantidad, Vencido, Consigna, Pagos, Total). FASE 4:
/// datos reales del endpoint.
/// </summary>
public class SorteoEstadoCuenta
{
    /// <summary>Etiqueta del sorteo (numero_sorteo, ej. "MAYOR-2025").</summary>
    public string Sorteo { get; set; } = string.Empty;

    /// <summary>Fecha del primer documento de consignación del grupo.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Fecha en formato de pantalla "26-octubre-2025" (es-MX fijo).</summary>
    public string FechaLarga => Helpers.FormatosFecha.FechaLarga(Fecha);

    /// <summary>Piezas históricamente consignadas del sorteo.</summary>
    public int Cantidad { get; set; }

    /// <summary>Importe vencido por fecha (sin descontar pagos).</summary>
    public decimal Vencido { get; set; }

    /// <summary>Importe de consigna.</summary>
    public decimal Consigna { get; set; }

    /// <summary>Importe de pagos aplicados.</summary>
    public decimal Pagos { get; set; }

    /// <summary>Total de la fila (Consigna − Pagos).</summary>
    public decimal Total { get; set; }
}
