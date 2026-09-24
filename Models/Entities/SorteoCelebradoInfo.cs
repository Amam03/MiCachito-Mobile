namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Registro de una entrega/devolución de material de un sorteo
/// (tarjeta de folio del mockup 4.1): folio, series, subtotal, ISR,
/// comisión, FDA y total. FASE 2 INTEGRACIÓN: datos reales de GET
/// api/mobile/sorteos/{id} — ISR/FDA vienen de movimientos_almacen_detalle
/// y la comisión calculada con el porcentaje del billetero.
/// </summary>
public class FolioEntregaSorteo
{
    public string Folio { get; init; } = string.Empty;

    /// <summary>CONSIGNA | DEVOLUCION (mayúsculas, para el badge).</summary>
    public string Movimiento { get; init; } = string.Empty;

    /// <summary>Fecha del movimiento ("dd-MM-yyyy").</summary>
    public string Fecha { get; init; } = string.Empty;

    /// <summary>Series entregadas (piezas).</summary>
    public string Series { get; init; } = string.Empty;

    public decimal Subtotal { get; init; }

    public decimal Isr { get; init; }

    public decimal Comision { get; init; }

    public decimal Fda { get; init; }

    public decimal Total { get; init; }
}

/// <summary>
/// Registro de un pago realizado del sorteo (tabla del mockup 4.1):
/// fecha, folio, monto y tipo. FASE 2: con aplicado_a para distinguir
/// los pagos hechos al momento del sorteo.
/// </summary>
public class PagoSorteo
{
    public string Fecha { get; init; } = string.Empty;

    public string Folio { get; init; } = string.Empty;

    public decimal Monto { get; init; }

    /// <summary>Tipo de pago (EFECTIVO, TRANSFERENCIA...).</summary>
    public string Tipo { get; init; } = string.Empty;

    /// <summary>Aplicación del pago (sorteo_vigente | sorteo_celebrado |
    /// consignas) — pagos al momento del sorteo = sorteo_*.</summary>
    public string AplicadoA { get; init; } = string.Empty;

    /// <summary>True si el pago se hizo al momento del sorteo.</summary>
    public bool AlMomento => AplicadoA is "sorteo_vigente" or "sorteo_celebrado";
}

/// <summary>
/// Información agregada de una dotación de sorteo (pantalla Sorteos,
/// mockups 4, 4.1, 4.2): resumen, entregas/devoluciones y pagos.
/// FASE 2 INTEGRACIÓN (2026-09-23): datos reales de GET api/mobile/sorteos
/// y api/mobile/sorteos/{id}?numero_sorteo=X. Una fila por DOTACIÓN
/// (numero_sorteo de recepción); los totales derivados se calculan sobre
/// las listas reales; saldo y pagos al momento vienen del backend.
/// </summary>
public class SorteoCelebradoInfo
{
    /// <summary>Id del sorteo (para el detalle).</summary>
    public int IdSorteo { get; init; }

    /// <summary>Número de dotación/recepción (null = fila sin número).</summary>
    public string? NumeroSorteo { get; init; }

    /// <summary>Label de la dotación ("MAYOR - 4024" o "MAYOR").</summary>
    public string Sorteo { get; init; } = string.Empty;

    /// <summary>Fecha del sorteo (fecha del último registro).</summary>
    public DateTime Fecha { get; init; }

    /// <summary>Fecha larga es-MX ("31-agosto-2026").</summary>
    public string FechaLarga => Helpers.FormatosFecha.FechaLarga(Fecha);

    // ── Resumen ──────────────────────────────────────────────────────

    /// <summary>Consignado (total de folios CONSIGNA).</summary>
    public decimal Consignado { get; init; }

    /// <summary>Devuelto (total de folios DEVOLUCION).</summary>
    public decimal Devuelto { get; init; }

    /// <summary>Ventas = consignado − devuelto (del backend).</summary>
    public decimal Ventas { get; init; }

    /// <summary>Pagos vivos totales (del backend).</summary>
    public decimal PagosRealizados { get; init; }

    /// <summary>Saldo = ventas − pagos (del backend; puede ser negativo).</summary>
    public decimal SaldoAPagar { get; init; }

    /// <summary>Pagos hechos al momento del sorteo (del backend).</summary>
    public decimal PagosAlMomento { get; init; }

    // ── Entregas y Devoluciones ──────────────────────────────────────

    public IReadOnlyList<FolioEntregaSorteo> Entregas { get; init; } = [];

    /// <summary>Total de folios de CONSIGNA.</summary>
    public decimal TotalEntregas => Entregas.Where(e => e.Movimiento == "CONSIGNA").Sum(e => e.Total);

    /// <summary>Total de folios de DEVOLUCION.</summary>
    public decimal TotalDevolucion => Entregas.Where(e => e.Movimiento == "DEVOLUCION").Sum(e => e.Total);

    /// <summary>Ventas del sorteo (misma cifra del resumen).</summary>
    public decimal VentasEntregas => Ventas;

    // ── Pagos Realizados ──────────────────────────────────────────────

    public IReadOnlyList<PagoSorteo> Pagos { get; init; } = [];

    public decimal TotalPagado => Pagos.Sum(p => p.Monto);
}
