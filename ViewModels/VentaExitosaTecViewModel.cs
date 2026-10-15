using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Venta Exitosa" (tras Confirmar Venta de
/// Sorteos Tec). Muestra el FOLIO y el TOTAL REALES devueltos por el
/// backend (POST api/mobile/ventas/crear), con el mismo patrón de
/// QueryProperty que la pantalla de éxito de Lotería Nacional.
///
/// No existe mockup: propuesta simple aprobada por el usuario (círculo
/// verde con check, "Venta Exitosa", datos de la venta, botón Continuar
/// que regresa al Home).
/// </summary>
[QueryProperty(nameof(Folio), "folio")]
[QueryProperty(nameof(TotalTexto), "total")]
public partial class VentaExitosaTecViewModel : BaseViewModel
{
    public VentaExitosaTecViewModel()
    {
        Title = "Venta Exitosa";
    }

    /// <summary>Folio real de la venta, devuelto por el backend.</summary>
    [ObservableProperty]
    private string folio = string.Empty;

    /// <summary>
    /// Total real formateado ("$140.00"). Lo calcula el backend y lo
    /// formatea la pantalla anterior al navegar; Mobile no recalcula nada.
    /// </summary>
    [ObservableProperty]
    private string totalTexto = string.Empty;

    /// <summary>
    /// Botón "Continuar": regresa a la pestaña Vender (Home).
    ///
    /// El Home es un ShellContent del TabBar SIN ruta global:
    /// GoToAsync("//HomePage") CRASHEA (ArgumentException, pitfall
    /// documentado y resuelto en VentaExitosaLotenalViewModel) — se
    /// regresa con pops relativos hasta la raíz.
    ///
    /// A diferencia de LOTENAL (stack fijo de 5 pushes), el flujo TEC
    /// tiene DOS caminos al carrito (desde SeleccionarBillete o directo
    /// desde SorteosTec), así que la profundidad varía: se hace POP de
    /// todo lo empujado sobre la raíz con NavigationStack.Count (mismo
    /// efecto que "../../../.." pero sin número mágico que rompa si el
    /// flujo gana una pantalla).
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
                // Pop de todo lo empujado sobre la pestaña raíz (Home):
                // "../.." repetido |pops| veces (pops=2 → "../..").
                int pops = Shell.Current.Navigation.NavigationStack.Count - 1;
                if (pops > 0)
                {
                    string ruta = string.Join(
                        "/", Enumerable.Repeat("..", pops));
                    await Shell.Current.GoToAsync(ruta);
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
