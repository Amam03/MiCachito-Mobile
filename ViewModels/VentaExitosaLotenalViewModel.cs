using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Venta Exitosa LOTENAL" (F3): se llega tras
/// confirmar la venta en el carrito (pantalla 11). Muestra el FOLIO y
/// el TOTAL REALES devueltos por el backend (POST
/// api/mobile/ventas/crear) más el resumen de boletos vendidos.
///
/// Sin mockup específico: sigue el patrón aprobado de VentaExitosaTec
/// (círculo verde con check + botón Continuar al Home), añadiendo los
/// datos reales de la venta (folio/total/boletos). El usuario la
/// ajustará al inspeccionarla.
/// </summary>
[QueryProperty(nameof(Folio), "folio")]
[QueryProperty(nameof(TotalTexto), "total")]
[QueryProperty(nameof(BoletosTexto), "boletos")]
public partial class VentaExitosaLotenalViewModel : BaseViewModel
{
    /// <summary>Folio real de la venta ("V-20260925-67242").</summary>
    [ObservableProperty]
    private string folio = string.Empty;

    /// <summary>Total real formateado ("$140.00").</summary>
    [ObservableProperty]
    private string totalTexto = string.Empty;

    /// <summary>Resumen de boletos vendidos ("2 boletos").</summary>
    [ObservableProperty]
    private string boletosTexto = string.Empty;

    public VentaExitosaLotenalViewModel()
    {
        Title = "Venta Exitosa";
    }

    /// <summary>
    /// Botón "Continuar": limpia la venta en curso (SesionVentaLotenal)
    /// y regresa al Home (raíz del Shell, patrón VentaExitosaTec).
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
            Data.SesionVentaLotenal.Limpiar();

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
