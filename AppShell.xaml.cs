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
    }
}