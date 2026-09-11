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
        Routing.RegisterRoute(nameof(CarritoComprasPage), typeof(CarritoComprasPage));
        Routing.RegisterRoute(nameof(SorteosTecPage), typeof(SorteosTecPage));
        Routing.RegisterRoute(nameof(SeleccionarBilletePage), typeof(SeleccionarBilletePage));
        Routing.RegisterRoute(nameof(CarritoTecPage), typeof(CarritoTecPage));
        Routing.RegisterRoute(nameof(DatosClienteTecPage), typeof(DatosClienteTecPage));
        Routing.RegisterRoute(nameof(VentaExitosaTecPage), typeof(VentaExitosaTecPage));
        Routing.RegisterRoute(nameof(ConsultaPremiosPage), typeof(ConsultaPremiosPage));
        Routing.RegisterRoute(nameof(ResultadoConsultaPremiosPage), typeof(ResultadoConsultaPremiosPage));
        Routing.RegisterRoute(nameof(PremiosReintegrosPage), typeof(PremiosReintegrosPage));
        Routing.RegisterRoute(nameof(DetallePremiosReintegrosPage), typeof(DetallePremiosReintegrosPage));
        Routing.RegisterRoute(nameof(CapturaPremiosReintegrosPage), typeof(CapturaPremiosReintegrosPage));
        Routing.RegisterRoute(nameof(EscanearBoletosPage), typeof(EscanearBoletosPage));
        Routing.RegisterRoute(nameof(SorteosPage), typeof(SorteosPage));
        Routing.RegisterRoute(nameof(TicketsVentaPage), typeof(TicketsVentaPage));
        Routing.RegisterRoute(nameof(DetalleVentaPage), typeof(DetalleVentaPage));
        Routing.RegisterRoute(nameof(DevolucionesPage), typeof(DevolucionesPage));
        Routing.RegisterRoute(nameof(ListaSorteosDevolucionPage), typeof(ListaSorteosDevolucionPage));
        Routing.RegisterRoute(nameof(NuevaDevolucionPage), typeof(NuevaDevolucionPage));
        Routing.RegisterRoute(nameof(DesgloseDevolucionPage), typeof(DesgloseDevolucionPage));
        Routing.RegisterRoute(nameof(EscanearSeriesPage), typeof(EscanearSeriesPage));
        Routing.RegisterRoute(nameof(DepositosPage), typeof(DepositosPage));
        Routing.RegisterRoute(nameof(NuevoDepositoPage), typeof(NuevoDepositoPage));
        Routing.RegisterRoute(nameof(RecibosPagoPage), typeof(RecibosPagoPage));
        Routing.RegisterRoute(nameof(ReportesPage), typeof(ReportesPage));
        Routing.RegisterRoute(nameof(DetallePagoPage), typeof(DetallePagoPage));
        Routing.RegisterRoute(nameof(ExpendioFormPage), typeof(ExpendioFormPage));
    }
}