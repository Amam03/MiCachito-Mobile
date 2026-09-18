using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Respuesta de POST /api/mobile/premios/consultar (backend Yii2,
/// controllers/api/mobile/PremiosController.php).
/// Campos planos string/decimal — patrón DetallePago (propiedades planas
/// para bindings compilados). Tipos igual que el backend: texto y montos
/// planos; resultado es string ("GANADOR", "REINTEGRO", "NO GANADOR");
/// tiene_reintegro llega como 0/1 (tinyint MySQL) y se lee como int.
/// </summary>
public sealed class RespuestaPremioApi
{
    [JsonPropertyName("boleto")]
    public string Boleto { get; set; } = string.Empty;

    [JsonPropertyName("serie")]
    public string Serie { get; set; } = string.Empty;

    [JsonPropertyName("sorteo")]
    public string Sorteo { get; set; } = string.Empty;

    [JsonPropertyName("numero_sorteo")]
    public string NumeroSorteo { get; set; } = string.Empty;

    [JsonPropertyName("fecha_sorteo")]
    public string FechaSorteo { get; set; } = string.Empty;

    /// <summary>"GANADOR", "REINTEGRO" o "NO GANADOR".</summary>
    [JsonPropertyName("resultado")]
    public string Resultado { get; set; } = "NO GANADOR";

    [JsonPropertyName("premio_cachito")]
    public decimal PremioCachito { get; set; }

    [JsonPropertyName("premio_serie")]
    public decimal PremioSerie { get; set; }

    [JsonPropertyName("reintegro")]
    public decimal Reintegro { get; set; }

    [JsonPropertyName("reintegro_total")]
    public decimal ReintegroTotal { get; set; }

    /// <summary>Llega como true/false (bool PHP en el JSON del backend).</summary>
    [JsonPropertyName("tiene_reintegro")]
    public bool TieneReintegro { get; set; }

    [JsonPropertyName("tipo_premio")]
    public int? TipoPremio { get; set; }

    [JsonPropertyName("numero_vigesimos")]
    public int NumeroVigesimos { get; set; }

    [JsonPropertyName("fuente")]
    public string? Fuente { get; set; }

    [JsonPropertyName("estatus_premio")]
    public string? EstatusPremio { get; set; }

    /// <summary>''=verificado sin premio, SIN_SABANA, SIN_SORTEO, AMBIGUO.</summary>
    [JsonPropertyName("motivo")]
    public string? Motivo { get; set; }
}
