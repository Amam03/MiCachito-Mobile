using System.Net.Http.Headers;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MiCachito.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddHttpClient<IApiClient, ApiClient>((services, client) =>
            {
                client.BaseAddress = new Uri(AppSettings.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(AppSettings.TimeoutSeconds);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(Constants.JsonContentType));
            });

            builder.Services.AddTransient<IAuthService, AuthService>();

            builder.Services.AddSingleton<SessionService>();
            builder.Services.AddSingleton<ISessionService>(sp => sp.GetRequiredService<SessionService>());
            builder.Services.AddSingleton<IAuthTokenProvider>(sp => sp.GetRequiredService<SessionService>());

            builder.Services.AddSingleton<Navigation.INavigationService, Navigation.NavigationService>();

            builder.Services.AddTransient<ViewModels.LoginViewModel>();
            builder.Services.AddTransient<Views.LoginPage>();
            builder.Services.AddTransient<ViewModels.HomeViewModel>();
            builder.Services.AddTransient<Views.HomePage>();
            builder.Services.AddTransient<ViewModels.TiempoAireViewModel>();
            builder.Services.AddTransient<Views.TiempoAirePage>();
            builder.Services.AddTransient<ViewModels.MontosTiempoAireViewModel>();
            builder.Services.AddTransient<Views.MontosTiempoAirePage>();
            builder.Services.AddTransient<ViewModels.NumeroTelefonoTiempoAireViewModel>();
            builder.Services.AddTransient<Views.NumeroTelefonoTiempoAirePage>();
            builder.Services.AddTransient<ViewModels.SorteosLotenalViewModel>();
            builder.Services.AddTransient<Views.SorteosLotenalPage>();
            builder.Services.AddTransient<ViewModels.SorteosActivosViewModel>();
            builder.Services.AddTransient<Views.SorteosActivosPage>();
            builder.Services.AddTransient<ViewModels.SeleccionCiudadViewModel>();
            builder.Services.AddTransient<Views.SeleccionCiudadPage>();
            builder.Services.AddTransient<ViewModels.AgregarBoletosViewModel>();
            builder.Services.AddTransient<Views.AgregarBoletosPage>();
            builder.Services.AddTransient<ViewModels.CarritoComprasViewModel>();
            builder.Services.AddTransient<Views.CarritoComprasPage>();
            builder.Services.AddTransient<ViewModels.SorteosTecViewModel>();
            builder.Services.AddTransient<Views.SorteosTecPage>();
            builder.Services.AddTransient<ViewModels.SeleccionarBilleteViewModel>();
            builder.Services.AddTransient<Views.SeleccionarBilletePage>();
            builder.Services.AddTransient<ViewModels.CarritoTecViewModel>();
            builder.Services.AddTransient<Views.CarritoTecPage>();
            builder.Services.AddTransient<ViewModels.DatosClienteTecViewModel>();
            builder.Services.AddTransient<Views.DatosClienteTecPage>();
            builder.Services.AddTransient<ViewModels.VentaExitosaTecViewModel>();
            builder.Services.AddTransient<Views.VentaExitosaTecPage>();
            builder.Services.AddTransient<ViewModels.GestionViewModel>();
            builder.Services.AddTransient<Views.GestionPage>();
            builder.Services.AddSingleton<Services.Scanning.ConsultaPremiosService>();
            builder.Services.AddSingleton<Services.Scanning.QrDecoderService>();
            builder.Services.AddTransient<ViewModels.ConsultaPremiosViewModel>();
            builder.Services.AddTransient<Views.ConsultaPremiosPage>();
            builder.Services.AddTransient<ViewModels.ResultadoConsultaPremiosViewModel>();
            builder.Services.AddTransient<Views.ResultadoConsultaPremiosPage>();
            builder.Services.AddSingleton<Services.PremiosReintegrosService>();
            builder.Services.AddTransient<ViewModels.PremiosReintegrosViewModel>();
            builder.Services.AddTransient<Views.PremiosReintegrosPage>();
            builder.Services.AddTransient<ViewModels.DetallePremiosReintegrosViewModel>();
            builder.Services.AddTransient<Views.DetallePremiosReintegrosPage>();
            builder.Services.AddTransient<ViewModels.CapturaPremiosReintegrosViewModel>();
            builder.Services.AddTransient<Views.CapturaPremiosReintegrosPage>();
            builder.Services.AddTransient<ViewModels.EscanearBoletosViewModel>();
            builder.Services.AddTransient<Views.EscanearBoletosPage>();
            builder.Services.AddTransient<Services.SorteosService>();
            builder.Services.AddTransient<ViewModels.SorteosViewModel>();
            builder.Services.AddTransient<Views.SorteosPage>();
            builder.Services.AddSingleton<Services.TicketsVentaService>();
            builder.Services.AddSingleton<Services.TicketPdfService>();
            builder.Services.AddSingleton<Services.IImpresoraService>(
                new Platforms.Android.Services.ImpresoraService());
            builder.Services.AddTransient<ViewModels.TicketsVentaViewModel>();
            builder.Services.AddTransient<Views.TicketsVentaPage>();
            builder.Services.AddTransient<ViewModels.DetalleVentaViewModel>();
            builder.Services.AddTransient<Views.DetalleVentaPage>();
            builder.Services.AddSingleton<Services.DevolucionService>();
            builder.Services.AddTransient<ViewModels.DevolucionesViewModel>();
            builder.Services.AddTransient<Views.DevolucionesPage>();
            builder.Services.AddTransient<ViewModels.ListaSorteosDevolucionViewModel>();
            builder.Services.AddTransient<Views.ListaSorteosDevolucionPage>();
            builder.Services.AddTransient<ViewModels.NuevaDevolucionViewModel>();
            builder.Services.AddTransient<Views.NuevaDevolucionPage>();
            builder.Services.AddTransient<ViewModels.DesgloseDevolucionViewModel>();
            builder.Services.AddTransient<Views.DesgloseDevolucionPage>();
            builder.Services.AddTransient<ViewModels.EscanearSeriesViewModel>();
            builder.Services.AddTransient<Views.EscanearSeriesPage>();
            builder.Services.AddSingleton<Services.DepositoService>();
            builder.Services.AddTransient<ViewModels.DepositosViewModel>();
            builder.Services.AddTransient<Views.DepositosPage>();
            builder.Services.AddTransient<ViewModels.NuevoDepositoViewModel>();
            builder.Services.AddTransient<Views.NuevoDepositoPage>();
            builder.Services.AddSingleton<Services.RecibosPagoService>();
            builder.Services.AddSingleton<Services.ReciboPagoPdfService>();
            builder.Services.AddSingleton<Services.EstadoCuentaService>();
            builder.Services.AddSingleton<Services.EstadoDeCuentaPdfService>();
            builder.Services.AddSingleton<Services.FondoAhorroService>();
            builder.Services.AddSingleton<Services.FondoAhorroPdfService>();
            builder.Services.AddSingleton<Services.FacturacionService>();
            builder.Services.AddSingleton<Services.FacturacionPdfService>();
            builder.Services.AddTransient<ViewModels.RecibosPagoViewModel>();
            builder.Services.AddTransient<Views.RecibosPagoPage>();
            builder.Services.AddTransient<ViewModels.ReportesViewModel>();
            builder.Services.AddTransient<Views.ReportesPage>();
            builder.Services.AddTransient<ViewModels.DetallePagoViewModel>();
            builder.Services.AddTransient<Views.DetallePagoPage>();
            builder.Services.AddTransient<ViewModels.ExpendiosViewModel>();
            builder.Services.AddTransient<Views.ExpendiosPage>();
            builder.Services.AddTransient<ViewModels.ExpendioFormViewModel>();
            builder.Services.AddTransient<Views.ExpendioFormPage>();

#if ANDROID
            builder.ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler(typeof(Controls.CameraScannerView),
                    typeof(Platforms.Android.Handlers.CameraScannerHandler));
            });
#endif
            builder.Services.AddTransient<Views.CuentaPage>();
            builder.Services.AddTransient<Views.SplashPage>();
            builder.Services.AddTransient<AppShell>();

            return builder.Build();
        }
    }
}
