using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Responses;

/// <summary>Respuesta del latido de captura en curso (Ticket 2-22).</summary>
public sealed class CapturaEnCursoApi
{
    [JsonPropertyName("id_sorteo")]
    public int IdSorteo { get; set; }

    [JsonPropertyName("cachitos")]
    public int Cachitos { get; set; }

    /// <summary>Segundos de vigencia del latido en el servidor.</summary>
    [JsonPropertyName("ttl_borrador_seg")]
    public int TtlBorradorSeg { get; set; }
}

/// <summary>Respuesta de cancelar el borrador (Ticket 2-22).</summary>
public sealed class CapturaEnCursoCanceladaApi
{
    [JsonPropertyName("borrados")]
    public int Borrados { get; set; }
}
