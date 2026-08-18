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
            builder.Services.AddTransient<Views.GestionPage>();
            builder.Services.AddTransient<Views.ExpendiosPage>();
            builder.Services.AddTransient<Views.CuentaPage>();
            builder.Services.AddTransient<Views.SplashPage>();
            builder.Services.AddTransient<AppShell>();

            return builder.Build();
        }
    }
}
