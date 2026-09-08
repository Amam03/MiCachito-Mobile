namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Registro de una entrega/devolución de material de un sorteo celebrado
/// (tarjeta de folio del mockup 4.1 expandido): folio, series, subtotal,
/// ISR, comisión, FDA y total. Fase SOLO INTERFAZ.
/// </summary>
public class FolioEntregaSorteo
{
    public string Folio { get; init; } = string.Empty;

    /// <summary>Series entregadas (ej. "Serie: 5-10").</summary>
    public string Series { get; init; } = string.Empty;

    public decimal Subtotal { get; init; }

    public decimal Isr { get; init; }

    public decimal Comision { get; init; }

    public decimal Fda { get; init; }

    public decimal Total { get; init; }
}

/// <summary>
/// Registro de un pago realizado del sorteo (tabla del mockup 4.1 pagos
/// expandido): fecha, folio, monto y tipo (ej. CAJA). Fase SOLO INTERFAZ.
/// </summary>
public class PagoSorteo
{
    public string Fecha { get; init; } = string.Empty;

    public string Folio { get; init; } = string.Empty;

    public decimal Monto { get; init; }

    /// <summary>Tipo de pago (mockups: "CAJA").</summary>
    public string Tipo { get; init; } = string.Empty;
}

/// <summary>
/// Información agregada de un sorteo celebrado para la pantalla Sorteos
/// (mockups 4, 4.1, 4.2): resumen, entregas/devoluciones y pagos.
/// Fase SOLO INTERFAZ: por decisión del usuario (2026-09-08) todas las
/// cifras van en $0.00 con listas de detalle vacías — el flujo visual
/// ya fue verificado; los cálculos reales llegarán con el backend
/// (ver docs/NOTAS_SORTEOS.md).
/// </summary>
public class SorteoCelebradoInfo
{
    public required SorteoActivoLotenal Sorteo { get; init; }

    // ── Resumen ──────────────────────────────────────────────────────
    public decimal Ventas { get; init; }

    public decimal PagosRealizados { get; init; }

    /// <summary>Ventas − Pagos realizados.</summary>
    public decimal SaldoAPagar => Ventas - PagosRealizados;

    // ── Entregas y Devoluciones ──────────────────────────────────────
    public IReadOnlyList<FolioEntregaSorteo> Entregas { get; init; } = [];

    public decimal TotalEntregas => Entregas.Sum(e => e.Total);

    public decimal TotalDevolucion { get; init; }

    /// <summary>Ventas del sorteo (misma cifra del resumen).</summary>
    public decimal VentasEntregas => Ventas;

    // ── Pagos Realizados ─────────────────────────────────────────────
    public IReadOnlyList<PagoSorteo> Pagos { get; init; } = [];

    public decimal TotalPagado => Pagos.Sum(p => p.Monto);
}
