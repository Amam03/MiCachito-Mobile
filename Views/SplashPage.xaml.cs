using MiCachito.Mobile.Api;
using MiCachito.Mobile.Navigation;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla de arranque: decide si hay sesión válida (auto-login) o se muestra el login.
/// Sin token -> Login. Con token -> api/mobile/auth/verify; ok -> Home, error -> borrar
/// token y Login (sesión expirada, revocada o credenciales rotadas vía CASCADE).
/// </summary>
public partial class SplashPage : ContentPage
{
    private readonly ISessionService _sessionService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private bool _started;

    public SplashPage(
        ISessionService sessionService,
        IAuthService authService,
        INavigationService navigationService)
    {
        InitializeComponent();

        _sessionService = sessionService;
        _authService = authService;
        _navigationService = navigationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_started)
        {
            return;
        }
        _started = true;

        Models.SessionInfo? session;
        try
        {
            session = await _sessionService.LoadAsync();
        }
        catch (Exception)
        {
            session = null;
        }

        if (session?.AccessToken is null)
        {
            await _navigationService.NavigateToLoginAsync();
            return;
        }

        try
        {
            var verify = await _authService.VerifyAsync();

            if (verify?.Expendio is null)
            {
                await _sessionService.ClearAsync();
                await _navigationService.NavigateToLoginAsync();
                return;
            }

            await _sessionService.UpdateAsync(verify);
            await _navigationService.NavigateToHomeAsync();
        }
        catch (ApiException)
        {
            // Token inválido, expirado o revocado: se borra la sesión y se pide login.
            try
            {
                await _sessionService.ClearAsync();
            }
            catch (Exception)
            {
            }
            await _navigationService.NavigateToLoginAsync();
        }
        catch (Exception)
        {
            // Sin conexión: se entra al Home con la sesión cacheada.
            await _navigationService.NavigateToHomeAsync();
        }
    }
}
