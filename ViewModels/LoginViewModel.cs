using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Navigation;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string usuario = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string mensaje = string.Empty;

    public LoginViewModel(
        IAuthService authService,
        ISessionService sessionService,
        INavigationService navigationService)
    {
        _authService = authService;
        _sessionService = sessionService;
        _navigationService = navigationService;
        Title = "Inicio de sesión";
    }

    [RelayCommand]
    private async Task IngresarAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            Mensaje = "Ingrese usuario y contraseña";
            return;
        }

        IsBusy = true;
        Mensaje = string.Empty;

        try
        {
            var response = await _authService.LoginAsync(Usuario.Trim(), Password);

            if (response?.Usuario is null || string.IsNullOrEmpty(response.Token))
            {
                Mensaje = "No se pudo iniciar sesión. Intente de nuevo.";
                return;
            }

            await _sessionService.SaveAsync(response, rememberMe: false);
            await _navigationService.NavigateToHomeAsync();
        }
        catch (ApiException ex)
        {
            Mensaje = ex.ServerMessage ?? "Error de autenticación. Intente de nuevo.";
        }
        catch (Exception)
        {
            Mensaje = "No se pudo conectar con el servidor. Intente más tarde.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
