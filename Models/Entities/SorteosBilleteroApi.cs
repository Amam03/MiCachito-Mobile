using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Respuesta de GET api/mobile/sorteos (lista): sorteos registrados
/// del billetero de la sesión (UNION movimientos + cartera, histórico
/// conservado) con label de recepción y agregados por sorteo.
/// </summary>
public sealed class SorteosBilleteroApi
{
    /// <summary>Sorteos del billetero, fecha descendente.</summary>
    [JsonPropertyName("sorteos")]
    public List<SorteoBilleteroApi>? Sorteos { get; set; }
}

/// <summary>Fila de sorteo (dotación) de la lista.</summary>
public sealed class SorteoBilleteroApi
{
    /// <summary>Id del sorteo (para el detalle).</summary>
    [JsonPropertyName("id_sorteo")]
    public int IdSorteo { get; set; }

    /// <summary>Label "NOMBRE - NUMERO" (o solo NOMBRE sin dotación).</summary>
    [JsonPropertyName("sorteo")]
    public string Sorteo { get; set; } = string.Empty;

    /// <summary>Número de dotación/recepción (null = fila sin número,
    /// p. ej. documentos de cartera).</summary>
    [JsonPropertyName("numero_sorteo")]
    public string? NumeroSorteo { get; set; }

    /// <summary>Fecha cruda del backend ("yyyy-MM-dd" o datetime MySQL).</summary>
    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    /// <summary>Total consignado (movs consigna aplicadas).</summary>
    [JsonPropertyName("consignado")]
    public decimal Consignado { get; set; }

    /// <summary>Total devuelto (movs devolución aplicadas).</summary>
    [JsonPropertyName("devuelto")]
    public decimal Devuelto { get; set; }

    /// <summary>Pagos vivos aplicados al sorteo.</summary>
    [JsonPropertyName("pagado")]
    public decimal Pagado { get; set; }

    /// <summary>Total entregas/devoluciones − pagos (puede ser negativo).</summary>
    [JsonPropertyName("saldo")]
    public decimal Saldo { get; set; }
}

/// <summary>
/// Respuesta de GET api/mobile/sorteos/{id}?numero_sorteo=X: detalle
/// completo de una dotación (folios + pagos + resumen).
/// </summary>
public sealed class DetalleSorteoApi
{
    /// <summary>Id del sorteo.</summary>
    [JsonPropertyName("id_sorteo")]
    public int IdSorteo { get; set; }

    /// <summary>Número de dotación (null = fila "sin número").</summary>
    [JsonPropertyName("numero_sorteo")]
    public string? NumeroSorteo { get; set; }

    /// <summary>Label de la dotación (misma regla que la lista).</summary>
    [JsonPropertyName("sorteo")]
    public string Sorteo { get; set; } = string.Empty;

    /// <summary>Resumen con saldos y pagos al momento del sorteo.</summary>
    [JsonPropertyName("resumen")]
    public ResumenSorteoApi? Resumen { get; set; }

    /// <summary>Entregas y devoluciones por folio de movimiento.</summary>
    [JsonPropertyName("entregas_devoluciones")]
    public List<FolioEntregaApi>? EntregasDevoluciones { get; set; }

    /// <summary>Pagos realizados (con aplicado_a).</summary>
    [JsonPropertyName("pagos_realizados")]
    public List<PagoSorteoApi>? PagosRealizados { get; set; }
}

/// <summary>Resumen del detalle.</summary>
public sealed class ResumenSorteoApi
{
    /// <summary>Total consignado.</summary>
    [JsonPropertyName("consignado")]
    public decimal Consignado { get; set; }

    /// <summary>Total devuelto.</summary>
    [JsonPropertyName("devuelto")]
    public decimal Devuelto { get; set; }

    /// <summary>Ventas = consignado − devuelto (neto).</summary>
    [JsonPropertyName("ventas")]
    public decimal Ventas { get; set; }

    /// <summary>Total de pagos vivos.</summary>
    [JsonPropertyName("pagos")]
    public decimal Pagos { get; set; }

    /// <summary>Ventas − pagos (puede ser negativo por sobrepago).</summary>
    [JsonPropertyName("saldo")]
    public decimal Saldo { get; set; }

    /// <summary>Pagos hechos al momento del sorteo (aplicado_a
    /// sorteo_vigente | sorteo_celebrado).</summary>
    [JsonPropertyName("pagos_al_momento")]
    public decimal PagosAlMomento { get; set; }
}

/// <summary>
/// Folio de entrega/devolución (movimientos_almacen agrupado): serie,
/// subtotal, ISR/FDA reales del detalle, comisión y total.
/// </summary>
public sealed class FolioEntregaApi
{
    /// <summary>CONSIGNA | DEVOLUCION.</summary>
    [JsonPropertyName("movimiento")]
    public string Movimiento { get; set; } = string.Empty;

    /// <summary>Folio del movimiento.</summary>
    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>Fecha cruda del movimiento.</summary>
    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    /// <summary>Piezas (cantidad de series).</summary>
    [JsonPropertyName("series")]
    public string Series { get; set; } = string.Empty;

    /// <summary>Subtotal (sin comisión).</summary>
    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    /// <summary>Comisión calculada con el pct del billetero.</summary>
    [JsonPropertyName("comision")]
    public decimal Comision { get; set; }

    /// <summary>Retención ISR real del detalle.</summary>
    [JsonPropertyName("ret_isr")]
    public decimal RetIsr { get; set; }

    /// <summary>Fondo de ahorro real del detalle.</summary>
    [JsonPropertyName("f_de_a")]
    public decimal FDeA { get; set; }

    /// <summary>Total = subtotal − comisión.</summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}

/// <summary>Pago realizado al sorteo (con aplicado_a).</summary>
public sealed class PagoSorteoApi
{
    /// <summary>Fecha cruda del pago.</summary>
    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    /// <summary>Folio de la ficha de pago.</summary>
    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>Monto del pago.</summary>
    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }

    /// <summary>Tipo de pago UPPER (EFECTIVO, TRANSFERENCIA...).</summary>
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Aplicación del pago (sorteo_vigente, sorteo_celebrado,
    /// consignas...).</summary>
    [JsonPropertyName("aplicado_a")]
    public string AplicadoA { get; set; } = string.Empty;
}
