using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Item de la lista de recibos de pago (GET api/mobile/pagos/recibos):
/// pagos APLICADOS por Caja del billetero de la sesión, agrupados por
/// movimiento (folio_grupo): una card por pago, monto = total del
/// movimiento (suma de TODAS sus formas de pago).
/// </summary>
public sealed class ReciboPagoItemApi
{
    [JsonPropertyName("id_ficha_pago")]
    public long IdFichaPago { get; set; }

    [JsonPropertyName("folio_ficha")]
    public string FolioFicha { get; set; } = string.Empty;

    /// <summary>efectivo | tarjeta | transferencia | cheque | deposito (forma representativa).</summary>
    [JsonPropertyName("tipo_pago")]
    public string TipoPago { get; set; } = string.Empty;

    /// <summary>Total del MOVIMIENTO (suma de todas las formas de pago del grupo).</summary>
    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }

    [JsonPropertyName("fecha_pago")]
    public string? FechaPago { get; set; }

    [JsonPropertyName("fecha_aplicacion")]
    public string? FechaAplicacion { get; set; }

    /// <summary>Fecha para la card: "28-octubre-2025" (aplicación si hay; si no, fecha de pago).</summary>
    public string FechaTexto
    {
        get
        {
            string? cruda = FechaAplicacion ?? FechaPago;
            if (Helpers.FormatosFecha.TryParseFechaBackend(cruda, out DateOnly f))
            {
                return Helpers.FormatosFecha.FechaLarga(f);
            }
            return cruda ?? string.Empty;
        }
    }

    /// <summary>Monto para la card: "$15,400.00".</summary>
    public string MontoTexto => $"${Monto.ToString("N2", System.Globalization.CultureInfo.CurrentCulture)}";
}

/// <summary>
/// Detalle de un recibo de pago (GET api/mobile/pagos/recibo/{id}): ficha
/// aplicada + desglose del movimiento (observaciones JSON que escribe
/// Escritorio en "B. Recepción de Pagos") con las filas listas para la UI
/// del mockup 8.1: formas de pago (positivas) y documentos de cartera
/// pagados (negativos "FACTURA LN", referencia "SORTEO --> FOLIO X").
/// </summary>
public sealed class ReciboPagoDetalleApi
{
    [JsonPropertyName("id_ficha_pago")]
    public long IdFichaPago { get; set; }

    [JsonPropertyName("folio_ficha")]
    public string FolioFicha { get; set; } = string.Empty;

    [JsonPropertyName("tipo_pago")]
    public string TipoPago { get; set; } = string.Empty;

    /// <summary>Monto de la ficha consultada (una forma de pago del movimiento).</summary>
    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }

    /// <summary>Total del MOVIMIENTO completo (header del mockup 8.1).</summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    /// <summary>Identificador del movimiento compuesto (fichas que comparten observaciones).</summary>
    [JsonPropertyName("folio_grupo")]
    public string? FolioGrupo { get; set; }

    [JsonPropertyName("fecha_pago")]
    public string? FechaPago { get; set; }

    [JsonPropertyName("fecha_aplicacion")]
    public string? FechaAplicacion { get; set; }

    /// <summary>Concepto Caja: consignas | sorteo_vigente | venta_electronica | ...</summary>
    [JsonPropertyName("aplicado_a")]
    public string? AplicadoA { get; set; }

    [JsonPropertyName("referencia_bancaria")]
    public string? ReferenciaBancaria { get; set; }

    [JsonPropertyName("estatus")]
    public string? Estatus { get; set; }

    /// <summary>Cliente para el PDF (nombre del billetero).</summary>
    [JsonPropertyName("cliente")]
    public string Cliente { get; set; } = string.Empty;

    /// <summary>CEDIS emisor para el PDF "(CEDIS ...)" (ya trae el prefijo desde la BD).</summary>
    [JsonPropertyName("cedis")]
    public string Cedis { get; set; } = string.Empty;

    /// <summary>Filas de formas de pago del desglose (mockup 8.1, montos positivos).</summary>
    [JsonPropertyName("formas")]
    public List<FilaDesgloseApi>? Formas { get; set; }

    /// <summary>Filas de documentos de cartera pagados (mockup 8.1, montos a mostrar negativos).</summary>
    [JsonPropertyName("documentos")]
    public List<DocumentoPagoApi>? Documentos { get; set; }

    /// <summary>Totales crudos por forma que escribió Escritorio (PDF, tabla Formas de Pago).</summary>
    [JsonPropertyName("desglose")]
    public Dictionary<string, decimal>? Desglose { get; set; }

    /// <summary>Depósitos/transferencias del movimiento: {tipo_pago, referencia, folio_movimiento, monto, ...}.</summary>
    [JsonPropertyName("depositos_transferencias_detalle")]
    public List<DepositoDetalleApi>? DepositosTransferenciasDetalle { get; set; }

    [JsonPropertyName("cheques_detalle")]
    public List<ChequeDetalleApi>? ChequesDetalle { get; set; }

    /// <summary>Compat PDF (mockup 8.3): documentos pagados con folio/tipo/monto.</summary>
    [JsonPropertyName("documentos_pagados")]
    public List<DocumentoPagadoApi>? DocumentosPagados { get; set; }

    /// <summary>Total a mostrar: total del movimiento; si el backend no lo trae, el de la ficha.</summary>
    public decimal TotalMovimiento => Total != 0m ? Total : Monto;
}

/// <summary>Fila del desglose (mockup 8.1): Descripción / Referencia / Monto.</summary>
public sealed class FilaDesgloseApi
{
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }
}

/// <summary>
/// Documento de cartera pagado por el movimiento (cartera_billeteros,
/// enriquecido con el sorteo). La UI lo muestra como fila "FACTURA LN"
/// negativa con referencia "NUMSORTEO --> FOLIO folio_documento".
/// </summary>
public sealed class DocumentoPagoApi
{
    [JsonPropertyName("id_cartera")]
    public long IdCartera { get; set; }

    [JsonPropertyName("monto_pago")]
    public decimal MontoPago { get; set; }

    [JsonPropertyName("folio_documento")]
    public string FolioDocumento { get; set; } = string.Empty;

    /// <summary>consignacion | venta | entrega | devolucion | ...</summary>
    [JsonPropertyName("tipo_documento")]
    public string TipoDocumento { get; set; } = string.Empty;

    [JsonPropertyName("id_sorteo")]
    public long? IdSorteo { get; set; }

    /// <summary>Referencia formateada: "MAYOR-2025 --> FOLIO CONS-20260225-001".</summary>
    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;
}

/// <summary>Depósito/transferencia del desglose (Escritorio: depositos_transferencias_dialog).</summary>
public sealed class DepositoDetalleApi
{
    [JsonPropertyName("tipo_pago")]
    public string TipoPago { get; set; } = string.Empty;

    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("folio_movimiento")]
    public string FolioMovimiento { get; set; } = string.Empty;

    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }

    [JsonPropertyName("cuenta_destino")]
    public string? CuentaDestino { get; set; }
}

/// <summary>Cheque del desglose (Escritorio: cheques_dialog).</summary>
public sealed class ChequeDetalleApi
{
    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }
}

/// <summary>Documento pagado (compat tabla del PDF, mockup 8.3).</summary>
public sealed class DocumentoPagadoApi
{
    [JsonPropertyName("folio_documento")]
    public string FolioDocumento { get; set; } = string.Empty;

    /// <summary>consignacion | venta | ...</summary>
    [JsonPropertyName("tipo_documento")]
    public string TipoDocumento { get; set; } = string.Empty;

    [JsonPropertyName("nombre_sorteo")]
    public string? NombreSorteo { get; set; }

    [JsonPropertyName("monto_pago")]
    public decimal MontoPago { get; set; }
}
