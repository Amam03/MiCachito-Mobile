using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Respuesta de POST api/mobile/depositos: el depósito registrado por el
/// backend (folio servidor DEP-YYYYMMDD-NNNN, estatus 'pendiente' hasta
/// que Caja lo valide desde Escritorio).
/// </summary>
public sealed class DepositoCreadoApi
{
    [JsonPropertyName("id_deposito")]
    public int IdDeposito { get; set; }

    [JsonPropertyName("folio_deposito")]
    public string FolioDeposito { get; set; } = string.Empty;

    /// <summary>Siempre 'pendiente' al registrar.</summary>
    [JsonPropertyName("estatus")]
    public string Estatus { get; set; } = "pendiente";
}
