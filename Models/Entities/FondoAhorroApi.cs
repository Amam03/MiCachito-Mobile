using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Respuesta de GET api/mobile/reportes/fondo-ahorro?fecha_inicio&amp;fecha_fin
/// (mockup 9.2): resumen del periodo + movimientos del billetero de la
/// sesión. Los montos llegan SIEMPRE positivos; el signo lo deriva la app
/// del origen (aportacion | retiro) al mapear a la entidad de dominio
/// (FondoAhorroService).
/// </summary>
public sealed class FondoAhorroApi
{
    /// <summary>Nombre del titular (billetero de la sesión).</summary>
    [JsonPropertyName("titular")]
    public string Titular { get; set; } = string.Empty;

    /// <summary>Saldo acumulado antes del rango consultado.</summary>
    [JsonPropertyName("saldo_inicial")]
    public decimal SaldoInicial { get; set; }

    /// <summary>Suma de depósitos del rango.</summary>
    [JsonPropertyName("depositos")]
    public decimal Depositos { get; set; }

    /// <summary>Suma de retiros del rango.</summary>
    [JsonPropertyName("retiros")]
    public decimal Retiros { get; set; }

    /// <summary>Saldo al final del rango (inicial + depósitos − retiros).</summary>
    [JsonPropertyName("saldo_final")]
    public decimal SaldoFinal { get; set; }

    /// <summary>Movimientos del rango en orden cronológico.</summary>
    [JsonPropertyName("movimientos")]
    public List<MovimientoFondoAhorroApi>? Movimientos { get; set; }
}

/// <summary>
/// Fila de movimiento del periodo (tabla del PDF 9.2): folio, fecha,
/// origen, monto y descripción.
/// </summary>
public sealed class MovimientoFondoAhorroApi
{
    /// <summary>Folio de la operación (id_fondo_ahorro como texto).</summary>
    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>Fecha cruda del backend ("yyyy-MM-dd" o datetime de MySQL).</summary>
    [JsonPropertyName("fecha")]
    public string? Fecha { get; set; }

    /// <summary>Origen del movimiento: aportacion | retiro.</summary>
    [JsonPropertyName("origen")]
    public string Origen { get; set; } = string.Empty;

    /// <summary>Monto positivo; el signo lo deriva la app del origen.</summary>
    [JsonPropertyName("monto")]
    public decimal Monto { get; set; }

    /// <summary>Observaciones reales o etiqueta del backend ("Ahorro semanal"...).</summary>
    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;
}
