namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Boleto dentro de un sorteo de un movimiento de Premios y Reintegros:
/// fila de la tabla Billete | Signo | Vig. | Valor (mockup 3.2).
/// Fase solo-interfaz: los valores del ejemplo son referencia visual
/// temporal de las imagenes, no datos reales.
/// </summary>
public sealed class BoletoPremioReintegro
{
    public string NumeroBillete { get; init; } = string.Empty;

    /// <summary>Signo zodiacal (Zodiaco) o serie (Loteria Nacional); "-" si no aplica.</summary>
    public string Signo { get; init; } = "-";

    /// <summary>Vigesimo / fraccion del cachito; "-" si no se conoce.</summary>
    public string Vig { get; init; } = "-";

    public decimal Valor { get; init; }
}
