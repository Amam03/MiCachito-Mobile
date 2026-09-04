using MiCachito.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MiCachito.Mobile.Navigation;

/// <summary>
/// Reemplaza la página raíz de la ventana actual.
/// Home y Login son raíces distintas (Shell vs ContentPage), por eso no se navega
/// con Shell.GoToAsync sino asignando Window.Page.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public Task NavigateToHomeAsync()
    {
        GetWindow().Page = _services.GetRequiredService<AppShell>();
        return Task.CompletedTask;
    }

    public Task NavigateToLoginAsync()
    {
        GetWindow().Page = _services.GetRequiredService<LoginPage>();
        return Task.CompletedTask;
    }

    public Task NavigateToTiempoAireAsync()
    {
        // Navega dentro del Shell actual usando el sistema de rutas de MAUI.
        // La ruta se registra en AppShell.xaml.cs.
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync(nameof(Views.TiempoAirePage));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Navega a MontosTiempoAirePage pasando el Id del proveedor como parámetro.
    /// MontosTiempoAireViewModel lo recibe vía [QueryProperty("proveedorId")].
    /// </summary>
    public Task NavigateToMontosTiempoAireAsync(int proveedorId)
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync(
                $"{nameof(Views.MontosTiempoAirePage)}?proveedorId={proveedorId}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Navega a NumeroTelefonoTiempoAirePage pasando el Id del proveedor y el monto.
    /// </summary>
    public Task NavigateToNumeroTelefonoTiempoAireAsync(int proveedorId, decimal monto)
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync(
                $"{nameof(Views.NumeroTelefonoTiempoAirePage)}?proveedorId={proveedorId}&monto={monto}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Navega a SorteosLotenalPage dentro del Shell actual.
    /// </summary>
    public Task NavigateToSorteosLotenalAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync(nameof(Views.SorteosLotenalPage));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Navega a SorteosActivosPage pasando el Id del tipo de sorteo.
    /// SorteosActivosViewModel lo recibe vía [QueryProperty("tipoSorteoId")].
    /// </summary>
    public Task NavigateToSorteosActivosAsync(int tipoSorteoId)
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync(
                $"{nameof(Views.SorteosActivosPage)}?tipoSorteoId={tipoSorteoId}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Navega a SeleccionCiudadPage pasando el Id del sorteo activo y el Id
    /// del tipo de sorteo. SeleccionCiudadViewModel los recibe vía
    /// [QueryProperty("sorteoId")] y [QueryProperty("tipoSorteoId")].
    /// </summary>
    public Task NavigateToSeleccionCiudadAsync(int sorteoId, int tipoSorteoId)
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync(
                $"{nameof(Views.SeleccionCiudadPage)}?sorteoId={sorteoId}&tipoSorteoId={tipoSorteoId}");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Navega a AgregarBoletosPage pasando el Id del sorteo activo, el Id
    /// del tipo de sorteo y el Id de la ciudad (0 = "Cualquier ciudad").
    /// AgregarBoletosViewModel los recibe vía [QueryProperty("sorteoId")],
    /// [QueryProperty("tipoSorteoId")] y [QueryProperty("ciudadId")].
    /// </summary>
    public Task NavigateToAgregarBoletosAsync(int sorteoId, int tipoSorteoId, int ciudadId)
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync(
                $"{nameof(Views.AgregarBoletosPage)}?sorteoId={sorteoId}&tipoSorteoId={tipoSorteoId}&ciudadId={ciudadId}");
        }

        return Task.CompletedTask;
    }

    private static Window GetWindow()
    {
        var current = Application.Current
            ?? throw new InvalidOperationException("No hay una Application activa.");

        if (current.Windows.Count == 0)
        {
            throw new InvalidOperationException("No hay ventanas activas.");
        }

        return current.Windows[0];
    }
}
