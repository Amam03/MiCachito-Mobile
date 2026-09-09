using System.Collections.ObjectModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Reporte Estado de Cuenta (pestaña 1 de Reportes, mockups 9/9.1).
/// Fase SOLO INTERFAZ: todos los valores en cero y la lista de sorteos
/// SIN registros — la estructura queda lista para recibir datos reales
/// del backend (docs/NOTAS_REPORTES.md).
/// </summary>
public class EstadoCuenta
{
    /// <summary>Fecha de emisión del reporte.</summary>
    public DateTime FechaEmision { get; set; } = DateTime.Today;

    /// <summary>Nombre del vendedor/agente (vendrá de la sesión real).</summary>
    public string Vendedor { get; set; } = string.Empty;

    /// <summary>Plaza/Zona del vendedor (vendrá de la sesión real).</summary>
    public string Plaza { get; set; } = string.Empty;

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
    /// Sorteos del estado de cuenta. Fase solo-interfaz: VACÍA por
    /// directriz del usuario (sin mocks ni seeds); se llenará desde
    /// el backend.
    /// </summary>
    public ObservableCollection<SorteoEstadoCuenta> Sorteos { get; } = new();

    /// <summary>Total de la columna Vencido.</summary>
    public decimal VencidoTotal => Sorteos.Sum(s => s.Vencido);

    /// <summary>Total de la columna Consigna.</summary>
    public decimal ConsignaTotal => Sorteos.Sum(s => s.Consigna);

    /// <summary>Total de la columna Pagos.</summary>
    public decimal PagosTotal => Sorteos.Sum(s => s.Pagos);

    /// <summary>Total final (Vencido − Consigna − Pagos, como el mockup).</summary>
    public decimal TotalFinal => Sorteos.Sum(s => s.Total);
}

/// <summary>
/// Fila de sorteo del Estado de Cuenta (tabla del PDF: Sorteo, Fecha,
/// Cantidad, Vencido, Consigna, Pagos, Total). Plantilla para los datos
/// reales del backend.
/// </summary>
public class SorteoEstadoCuenta
{
    /// <summary>Nombre del sorteo (ej. "ZODIACO ESPECIAL 1724").</summary>
    public string Sorteo { get; set; } = string.Empty;

    /// <summary>Fecha de la operación.</summary>
    public DateTime Fecha { get; set; }

    /// <summary>Cantidad de la operación.</summary>
    public int Cantidad { get; set; }

    /// <summary>Importe vencido.</summary>
    public decimal Vencido { get; set; }

    /// <summary>Importe de consigna.</summary>
    public decimal Consigna { get; set; }

    /// <summary>Importe de pagos.</summary>
    public decimal Pagos { get; set; }

    /// <summary>Total de la fila.</summary>
    public decimal Total { get; set; }
}
