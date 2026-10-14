using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Responses;

// Contrato de GET/POST api/mobile/premios-reintegros (+/{id}) —
// Premios y Reintegros 3.x (Gestión). Scope: SOLO los movimientos del
// billetero de la sesión; detalle ajeno → 404. D1: solo boletos ACEPTADOS
// (premio/reintegro); D3: folio servidor PYR-YYYYMMDD-NNNN.

/// <summary>Boleto aceptado dentro de un grupo de sorteo del detalle (D2).</summary>
public sealed class BoletoPyRApi
{
    [JsonPropertyName("id_detalle")]
    public int IdDetalle { get; set; }

    /// <summary>Número impreso ("47727").</summary>
    [JsonPropertyName("numero_billete")]
    public string? NumeroBillete { get; set; }

    /// <summary>Signo zodiacal o null.</summary>
    [JsonPropertyName("signo")]
    public string? Signo { get; set; }

    /// <summary>Serie ("01") o null.</summary>
    [JsonPropertyName("serie")]
    public string? Serie { get; set; }

    /// <summary>Vigésimo/fracción como string ("5") o null.</summary>
    [JsonPropertyName("vig")]
    public string? Vig { get; set; }

    /// <summary>Valor del premio/reintegro del cachito.</summary>
    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    /// <summary>Código de barras completo (identidad del boleto).</summary>
    [JsonPropertyName("codigo_barras")]
    public string? CodigoBarras { get; set; }
}

/// <summary>Grupo de un sorteo dentro de una pestaña (premios o reintegros) del detalle (D2).</summary>
public sealed class GrupoSorteoPyRApi
{
    /// <summary>Nombre del sorteo ("Mayor - 3966").</summary>
    [JsonPropertyName("nombre_sorteo")]
    public string NombreSorteo { get; set; } = string.Empty;

    /// <summary>Id del sorteo o null.</summary>
    [JsonPropertyName("id_sorteo")]
    public int? IdSorteo { get; set; }

    /// <summary>Boletos del grupo (orden de captura).</summary>
    [JsonPropertyName("boletos")]
    public List<BoletoPyRApi>? Boletos { get; set; }
}

/// <summary>Fila del histórico (mockup 3: folio + fecha + totales).</summary>
public sealed class MovimientoPyRApi
{
    /// <summary>PK del movimiento — clave de navegación al detalle.</summary>
    [JsonPropertyName("id_movimiento")]
    public int IdMovimiento { get; set; }

    /// <summary>Folio servidor "PYR-YYYYMMDD-NNNN" (D3).</summary>
    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>"yyyy-MM-dd HH:mm:ss".</summary>
    [JsonPropertyName("fecha_movimiento")]
    public string? FechaMovimiento { get; set; }

    [JsonPropertyName("total_premios")]
    public decimal TotalPremios { get; set; }

    [JsonPropertyName("total_reintegros")]
    public decimal TotalReintegros { get; set; }

    [JsonPropertyName("total_general")]
    public decimal TotalGeneral { get; set; }
}

/// <summary>Respuesta del POST (guardar): el movimiento creado.</summary>
public sealed class MovimientoCreadoPyRApi
{
    [JsonPropertyName("id_movimiento")]
    public int IdMovimiento { get; set; }

    /// <summary>Folio servidor PYR-YYYYMMDD-NNNN (D3).</summary>
    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    [JsonPropertyName("fecha_movimiento")]
    public string? FechaMovimiento { get; set; }

    [JsonPropertyName("total_premios")]
    public decimal TotalPremios { get; set; }

    [JsonPropertyName("total_reintegros")]
    public decimal TotalReintegros { get; set; }

    [JsonPropertyName("total_general")]
    public decimal TotalGeneral { get; set; }
}

/// <summary>Detalle completo (mockups 3.1/3.2): encabezado + vista agrupada por sorteo (D2).</summary>
public sealed class DetallePyRApi
{
    [JsonPropertyName("id_movimiento")]
    public int IdMovimiento { get; set; }

    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>"yyyy-MM-dd HH:mm:ss".</summary>
    [JsonPropertyName("fecha_movimiento")]
    public string? FechaMovimiento { get; set; }

    [JsonPropertyName("total_premios")]
    public decimal TotalPremios { get; set; }

    [JsonPropertyName("total_reintegros")]
    public decimal TotalReintegros { get; set; }

    [JsonPropertyName("total_general")]
    public decimal TotalGeneral { get; set; }

    /// <summary>Grupos de premios (pestaña Premios).</summary>
    [JsonPropertyName("premios")]
    public List<GrupoSorteoPyRApi>? Premios { get; set; }

    /// <summary>Grupos de reintegros (pestaña Reintegros).</summary>
    [JsonPropertyName("reintegros")]
    public List<GrupoSorteoPyRApi>? Reintegros { get; set; }
}
