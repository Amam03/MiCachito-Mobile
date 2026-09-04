using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Venta Exitosa" (tras Confirmar Venta de
/// Sorteos Tec). No existe mockup: propuesta simple aprobada por el
/// usuario (círculo verde con check, "Venta Exitosa", botón Continuar
/// que regresa al Home) — el usuario la ajustará al inspeccionarla.
/// </summary>
public partial class VentaExitosaTecViewModel : BaseViewModel
{
    public VentaExitosaTecViewModel()
    {
        Title = "Venta Exitosa";
    }

    /// <summary>
    /// Botón "Continuar": regresa al Home (raíz del Shell).
    /// </summary>
    [RelayCommand]
    private async Task ContinuarAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("//HomePage");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
