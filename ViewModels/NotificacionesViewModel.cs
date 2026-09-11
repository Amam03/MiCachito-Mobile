using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// Notificaciones (mockup 3). Fase SOLO INTERFAZ: no existen datos
/// reales de notificaciones — estado vacío SIN registros inventados
/// (regla del proyecto). El listado llegará con la integración del
/// backend.
/// </summary>
public partial class NotificacionesViewModel : BaseViewModel
{
    /// <summary>True mientras no haya notificaciones reales (estado vacío del mockup 3).</summary>
    [ObservableProperty]
    private bool _sinNotificaciones = true;

    /// <summary>Regresa a Cuenta (flecha del header, mockup 3).</summary>
    [RelayCommand]
    private async Task VolverAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
