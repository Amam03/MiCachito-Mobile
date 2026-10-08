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
