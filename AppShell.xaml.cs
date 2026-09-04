using MiCachito.Mobile.Views;

namespace MiCachito.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // Registrar rutas adicionales dentro del Shell.
        Routing.RegisterRoute(nameof(TiempoAirePage), typeof(TiempoAirePage));
        Routing.RegisterRoute(nameof(MontosTiempoAirePage), typeof(MontosTiempoAirePage));
        Routing.RegisterRoute(nameof(NumeroTelefonoTiempoAirePage), typeof(NumeroTelefonoTiempoAirePage));
        Routing.RegisterRoute(nameof(SorteosLotenalPage), typeof(SorteosLotenalPage));
        Routing.RegisterRoute(nameof(SorteosActivosPage), typeof(SorteosActivosPage));
        Routing.RegisterRoute(nameof(SeleccionCiudadPage), typeof(SeleccionCiudadPage));
        Routing.RegisterRoute(nameof(AgregarBoletosPage), typeof(AgregarBoletosPage));
    }
}