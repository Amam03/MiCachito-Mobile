using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Item del historial de depósitos (GET api/mobile/depositos): registros
/// del billetero de la sesión con el banco de la cuenta (JOIN). La nube
/// ámbar de la tarjeta (mockup 7.3) corresponde a estatus 'pendiente'.
/// </summary>
public sealed class DepositoApi
{
    [JsonPropertyName("id_deposito")]
    public int IdDeposito { get; set; }

    /// <summary>Folio servidor: "DEP-YYYYMMDD-NNNN".</summary>
    [JsonPropertyName("folio_deposito")]
    public string FolioDeposito { get; set; } = string.Empty;

    /// <summary>Banco de la cuenta (JOIN cuentas_bancarias.banco).</summary>
    [JsonPropertyName("banco")]
    public string Banco { get; set; } = string.Empty;

    [JsonPropertyName("id_cuenta_bancaria")]
    public int IdCuentaBancaria { get; set; }

    /// <summary>"yyyy-MM-dd" (fecha del depósito, no de la captura).</summary>
    [JsonPropertyName("fecha_deposito")]
    public string? FechaDeposito { get; set; }

    /// <summary>"HH:mm:ss" o null si no se capturó.</summary>
    [JsonPropertyName("hora_deposito")]
    public string? HoraDeposito { get; set; }

    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }

    /// <summary>Folio/Movimiento capturado en el formulario (referencia).</summary>
    [JsonPropertyName("referencia")]
    public string? Referencia { get; set; }

    [JsonPropertyName("observaciones")]
    public string? Observaciones { get; set; }

    /// <summary>pendiente | validado | rechazado | utilizado. La nube ámbar = pendiente.</summary>
    [JsonPropertyName("estatus")]
    public string Estatus { get; set; } = "pendiente";

    /// <summary>"yyyy-MM-dd HH:mm:ss" de la captura.</summary>
    [JsonPropertyName("fecha_creacion")]
    public string? FechaCreacion { get; set; }

    /// <summary>Fecha de captura parseada; null si el backend no la trae.</summary>
    public DateOnly? FechaCapturaValor =>
        Helpers.FormatosFecha.TryParseFechaBackend(FechaCreacion, out DateOnly f) ? f : null;

    /// <summary>Fecha de captura: "Captura: dd-MMMM-yyyy" (es-MX fijo).</summary>
    public string CapturaTexto =>
        FechaCapturaValor.HasValue
            ? $"Captura: {Helpers.FormatosFecha.FechaLarga(FechaCapturaValor.Value)}"
            : "Captura: -";

    /// <summary>Nube ámbar del mockup 7.3: el depósito aún no lo valida Caja.</summary>
    public bool Pendiente => Estatus == "pendiente";

    /// <summary>Fecha del depósito parseada; null si el backend no la trae.</summary>
    public DateOnly? FechaDepositoValor =>
        Helpers.FormatosFecha.TryParseFechaBackend(FechaDeposito, out DateOnly f) ? f : null;

    /// <summary>Hora del depósito parseada ("HH:mm"); null si no hay.</summary>
    public TimeOnly? HoraDepositoValor
    {
        get
        {
            string? cruda = HoraDeposito;
            if (string.IsNullOrWhiteSpace(cruda))
            {
                return null;
            }
            if (TimeOnly.TryParse(cruda[..Math.Min(cruda.Length, 5)], out TimeOnly h))
            {
                return h;
            }
            return null;
        }
    }

    /// <summary>Fecha y hora del depósito: "dd-MMMM-yyyy HH:mm" (es-MX fijo).</summary>
    public string FechaHoraTexto
    {
        get
        {
            string fecha = FechaDepositoValor.HasValue
                ? Helpers.FormatosFecha.FechaLarga(FechaDepositoValor.Value)
                : "-";
            string hora = HoraDepositoValor.HasValue ? $" {HoraDepositoValor:hh\\:mm}" : string.Empty;
            return $"{fecha}{hora}";
        }
    }

    /// <summary>Importe formateado con 2 decimales: "$1,500.50".</summary>
    public string MontoTexto => $"${Monto:0.00}";

    /// <summary>Línea de la tarjeta: "BANCO - $1,500.50" (mockup 7.3).</summary>
    public string BancoMontoTexto => $"{Banco} - {MontoTexto}";
}
