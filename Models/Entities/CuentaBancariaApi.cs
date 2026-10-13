using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Cuenta bancaria ACTIVA del catálogo (GET api/mobile/depositos/cuentas):
/// alimenta el selector de banco de Nuevo Depósito. Mismo shape que el
/// catálogo Desktop (api/cuentas-bancarias) con selección por id_cuenta.
/// </summary>
public sealed class CuentaBancariaApi
{
    [JsonPropertyName("id_cuenta")]
    public int IdCuenta { get; set; }

    /// <summary>Nombre corto del banco para el label del campo ("BBVA").</summary>
    [JsonPropertyName("banco")]
    public string Banco { get; set; } = string.Empty;

    [JsonPropertyName("numero_cuenta")]
    public string NumeroCuenta { get; set; } = string.Empty;

    /// <summary>"banco — numero_cuenta" (label del catálogo backend).</summary>
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;
}
