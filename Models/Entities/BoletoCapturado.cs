namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Tipo de boleto capturado en un movimiento de Premios y Reintegros.
/// </summary>
public enum TipoCapturaBoleto
{
    Premio,
    Reintegro,
}

/// <summary>
/// Boleto acumulado durante la captura (flujo 3.3): vive SOLO en memoria
/// en PremiosReintegrosService hasta Guardar (se convierte en fila de la
/// tabla del detalle) o Cancelar (se descarta). Fase solo-interfaz.
/// </summary>
public sealed class BoletoCapturado
{
    public TipoCapturaBoleto Tipo { get; init; }

    /// <summary>Codigo completo del QR/cadena: identidad para dedupe.</summary>
    public string CodigoCompleto { get; init; } = string.Empty;

    /// <summary>Nombre corto del sorteo para agrupar (ej. "Mayor - 4010").</summary>
    public string NombreSorteo { get; init; } = string.Empty;

    public string NumeroBillete { get; init; } = string.Empty;

    /// <summary>Signo (Zodiaco) o serie (LN) para la columna Signo del detalle.</summary>
    public string SignoOSerie { get; init; } = "-";

    /// <summary>Fraccion/vigesimo para la columna Vig. del detalle.</summary>
    public string Vig { get; init; } = "-";

    /// <summary>Valor del premio o reintegro (mock: $30.00 reintegro, referencia visual).</summary>
    public decimal Valor { get; init; }
}
