using CommunityToolkit.Mvvm.ComponentModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Representa un monto seleccionable en la pantalla de montos de Tiempo Aire.
/// UI-only: la selección es visual, sin procesamiento de recarga.
/// </summary>
public partial class MontoTiempoAire : ObservableObject
{
    /// <summary>
    /// Valor del monto (ej. 10, 20, 50, 100).
    /// </summary>
    public decimal Valor { get; init; }

    /// <summary>
    /// Texto a mostrar en el botón (ej. "$10", "$20").
    /// </summary>
    public string DisplayText => $"${Valor}";

    /// <summary>
    /// Indica si el monto está seleccionado visualmente.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;
}
