using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Requests;

/// <summary>
/// Latido de una devolución que el billetero está CAPTURANDO y aún no guarda
/// (Ticket 2-22, Monitor de Escritorio): POST api/mobile/devoluciones/en-captura.
/// El billetero lo determina el backend por la sesión; nunca se envía.
/// </summary>
public sealed class CapturaEnCursoRequest
{
    /// <summary>Sorteo (id_sorteo) de la devolución en curso.</summary>
    [JsonPropertyName("id_sorteo")]
    public int IdSorteo { get; set; }

    /// <summary>Boletos escaneados hasta ahora (informativo).</summary>
    [JsonPropertyName("cachitos")]
    public int Cachitos { get; set; }

    /// <summary>Dotaci&oacute;n (numero_sorteo) de la devoluci&oacute;n en curso; null = legacy sin dotaci&oacute;n.</summary>
    [JsonPropertyName("numero_sorteo")]
    public string? NumeroSorteo { get; set; }
}

/// <summary>Cancelación del borrador: POST api/mobile/devoluciones/en-captura/cancelar.</summary>
public sealed class CancelarCapturaEnCursoRequest
{
    /// <summary>Sorteo a limpiar; null = todos los borradores del billetero.</summary>
    [JsonPropertyName("id_sorteo")]
    public int? IdSorteo { get; set; }

    /// <summary>Dotaci&oacute;n a limpiar; null = todas las dotaciones del sorteo.</summary>
    [JsonPropertyName("numero_sorteo")]
    public string? NumeroSorteo { get; set; }
}
