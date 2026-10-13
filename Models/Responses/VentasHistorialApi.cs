using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Responses;

// Contrato de GET api/mobile/ventas (historial) y GET api/mobile/ventas/{id}
// (detalle) — Tickets de Venta 5.x (Gestión). Scope: SOLO las ventas del
// billetero de la sesión (el backend nunca acepta un id_billetero del
// request; el detalle de una venta ajena responde 404).

/// <summary>Cliente capturado en la venta (DatosClienteTecPage) o null.</summary>
public sealed class ClienteVentaRespuestaApi
{
    /// <summary>Nombre completo ("PERLA E2E TEC").</summary>
    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; }

    /// <summary>Teléfono o null.</summary>
    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    /// <summary>Correo o null.</summary>
    [JsonPropertyName("correo")]
    public string? Correo { get; set; }
}

/// <summary>Fila del historial de ventas del billetero (mockup 5).</summary>
public sealed class VentaHistorialApi
{
    /// <summary>PK de la venta (ventas.id_venta) — clave de navegación al detalle.</summary>
    [JsonPropertyName("id_venta")]
    public int IdVenta { get; set; }

    /// <summary>Folio de la venta ("1103" o "V-20260831-67707" legacy).</summary>
    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>'lotenal' | 'sorteos_tec' ('tiempo_aire' reservado a TAECEL).</summary>
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "lotenal";

    /// <summary>activa | pagada | cancelada | devuelta (D5: el historial trae TODAS).</summary>
    [JsonPropertyName("estatus")]
    public string Estatus { get; set; } = "activa";

    /// <summary>"yyyy-MM-dd HH:mm:ss" de la venta.</summary>
    [JsonPropertyName("fecha_venta")]
    public string? FechaVenta { get; set; }

    /// <summary>Total real de la venta (calculado por el backend).</summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    /// <summary>Id del sorteo de la primera partida; null en partidas legacy.</summary>
    [JsonPropertyName("id_sorteo")]
    public int? IdSorteo { get; set; }

    /// <summary>Nombre del sorteo ("Sorteo 041753", "Sorteo Mi Sueño").</summary>
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Cliente de la venta o null (la app muestra "PÚBLICO EN GENERAL").</summary>
    [JsonPropertyName("cliente")]
    public ClienteVentaRespuestaApi? Cliente { get; set; }
}

/// <summary>Detalle completo de una venta (mockup 5.1) + partidas.</summary>
public sealed class VentaDetalleApi
{
    [JsonPropertyName("id_venta")]
    public int IdVenta { get; set; }

    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>'lotenal' | 'sorteos_tec'.</summary>
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "lotenal";

    [JsonPropertyName("estatus")]
    public string Estatus { get; set; } = "activa";

    /// <summary>"yyyy-MM-dd HH:mm:ss" de la venta.</summary>
    [JsonPropertyName("fecha_venta")]
    public string? FechaVenta { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    /// <summary>Vendedor = billetero de la sesión (nombre completo).</summary>
    [JsonPropertyName("vendedor")]
    public string Vendedor { get; set; } = string.Empty;

    /// <summary>CEDIS del billetero (D1: Plaza se omite — sin ciudad en BD).</summary>
    [JsonPropertyName("cedis")]
    public string Cedis { get; set; } = string.Empty;

    [JsonPropertyName("cliente")]
    public ClienteVentaRespuestaApi? Cliente { get; set; }

    /// <summary>Partidas de la venta (ventas_detalle), en orden de captura.</summary>
    [JsonPropertyName("partidas")]
    public List<PartidaVentaApi>? Partidas { get; set; }
}

/// <summary>Una partida de la venta (una fracción/boleto vendido).</summary>
public sealed class PartidaVentaApi
{
    [JsonPropertyName("id_detalle_venta")]
    public int IdDetalleVenta { get; set; }

    /// <summary>PK del billete o null (partidas legacy sin trazabilidad).</summary>
    [JsonPropertyName("id_billete")]
    public int? IdBillete { get; set; }

    /// <summary>Número impreso ("0101", "TEC003") o null en legacy.</summary>
    [JsonPropertyName("numero_billete")]
    public string? NumeroBillete { get; set; }

    /// <summary>Serie física ("01") o null.</summary>
    [JsonPropertyName("serie")]
    public string? Serie { get; set; }

    /// <summary>Nombre de signo zodiacal o null.</summary>
    [JsonPropertyName("signo")]
    public string? Signo { get; set; }

    [JsonPropertyName("cantidad")]
    public int Cantidad { get; set; }

    [JsonPropertyName("precio_unitario")]
    public decimal PrecioUnitario { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    /// <summary>Dotación ("041753") o null.</summary>
    [JsonPropertyName("numero_sorteo")]
    public string? NumeroSorteo { get; set; }

    /// <summary>Nombre del sorteo de la partida.</summary>
    [JsonPropertyName("nombre_sorteo")]
    public string? NombreSorteo { get; set; }
}
