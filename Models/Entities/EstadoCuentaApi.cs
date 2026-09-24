using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Respuesta de GET api/mobile/reportes/estado-cuenta (mockup 9.1):
/// estado de cuenta histórico del billetero de la sesión. Las filas de
/// sorteos contienen SOLO documentos de consignación vivos agrupados por
/// sorteo (semántica validada en Fase 1: las ventas solo alimentan el
/// concepto Pagarés del resumen y al_corriente); totales y conceptos ya
/// vienen calculados por el backend — la app NO recalcula.
/// </summary>
public sealed class EstadoCuentaApi
{
    /// <summary>Identidad del billetero de la sesión.</summary>
    [JsonPropertyName("billetero")]
    public BilleteroEstadoCuentaApi? Billetero { get; set; }

    /// <summary>Fecha de emisión cruda del backend ("yyyy-MM-dd").</summary>
    [JsonPropertyName("fecha_emision")]
    public string? FechaEmision { get; set; }

    /// <summary>True si ningún documento vencido conserva saldo real.</summary>
    [JsonPropertyName("al_corriente")]
    public bool AlCorriente { get; set; }

    /// <summary>Bloque de resumen (6 conceptos).</summary>
    [JsonPropertyName("conceptos")]
    public ConceptosEstadoCuentaApi? Conceptos { get; set; }

    /// <summary>Filas por sorteo (solo consignaciones vivas).</summary>
    [JsonPropertyName("sorteos")]
    public List<SorteoEstadoCuentaApi>? Sorteos { get; set; }

    /// <summary>Totales de la tabla por sorteo.</summary>
    [JsonPropertyName("totales")]
    public TotalesEstadoCuentaApi? Totales { get; set; }
}

/// <summary>Identidad del billetero para el encabezado del PDF.</summary>
public sealed class BilleteroEstadoCuentaApi
{
    /// <summary>Nombre completo (Vendedor del PDF).</summary>
    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Nombre del CEDIS (Plaza del PDF).</summary>
    [JsonPropertyName("cedis")]
    public string Cedis { get; set; } = string.Empty;
}

/// <summary>Conceptos del bloque "Resumen de saldo".</summary>
public sealed class ConceptosEstadoCuentaApi
{
    /// <summary>Saldo real del último registro de fondo_ahorro.</summary>
    [JsonPropertyName("fondo_ahorro")]
    public decimal FondoDeAhorro { get; set; }

    /// <summary>Saldo pendiente de cartera tipo consignación.</summary>
    [JsonPropertyName("fideicomiso")]
    public decimal Fideicomiso { get; set; }

    /// <summary>Saldo pendiente de cartera tipo venta.</summary>
    [JsonPropertyName("pagares")]
    public decimal Pagares { get; set; }

    /// <summary>0 fijo hasta que exista fuente real (auditoría F1).</summary>
    [JsonPropertyName("bolsa_electronica")]
    public decimal BolsaElectronica { get; set; }

    /// <summary>FA + Fideicomiso + Pagarés + Bolsa Electrónica.</summary>
    [JsonPropertyName("garantia_total")]
    public decimal GarantiaTotal { get; set; }

    /// <summary>límite_credito − deuda total.</summary>
    [JsonPropertyName("capacidad_credito")]
    public decimal CapacidadDeCredito { get; set; }
}

/// <summary>
/// Fila por sorteo de la tabla 9.1 (Sorteo, Fecha, Cantidad, Vencido,
/// Consigna, Pagos, Total). Fecha = primer documento de consignación del
/// grupo; Cantidad = piezas históricamente consignadas.
/// </summary>
public sealed class SorteoEstadoCuentaApi
{
    /// <summary>Etiqueta del sorteo (numero_sorteo, fallback nombre).</summary>
    [JsonPropertyName("sorteo")]
    public string Sorteo { get; set; } = string.Empty;

    /// <summary>Fecha cruda del backend ("yyyy-MM-dd" o datetime MySQL).</summary>
    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    /// <summary>Piezas históricamente consignadas del sorteo.</summary>
    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    /// <summary>Monto vencido por fecha (sin descontar pagos).</summary>
    [JsonPropertyName("vencido")]
    public decimal Vencido { get; set; }

    /// <summary>Suma de monto_original de consignaciones del sorteo.</summary>
    [JsonPropertyName("consigna")]
    public decimal Consigna { get; set; }

    /// <summary>Suma viva de pagos_cartera aplicados al sorteo.</summary>
    [JsonPropertyName("pagos")]
    public decimal Pagos { get; set; }

    /// <summary>Consigna − Pagos (puede ser negativo por sobrepago).</summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}

/// <summary>Totales de las columnas de la tabla por sorteo.</summary>
public sealed class TotalesEstadoCuentaApi
{
    /// <summary>Total de la columna Vencido.</summary>
    [JsonPropertyName("vencido")]
    public decimal Vencido { get; set; }

    /// <summary>Total de la columna Consigna.</summary>
    [JsonPropertyName("consigna")]
    public decimal Consigna { get; set; }

    /// <summary>Total de la columna Pagos.</summary>
    [JsonPropertyName("pagos")]
    public decimal Pagos { get; set; }

    /// <summary>Total final (Consigna − Pagos).</summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}
