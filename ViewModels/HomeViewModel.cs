using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Navigation;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly ISessionService _sessionService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string tipoUsuario = string.Empty;

    [ObservableProperty]
    private string sede = string.Empty;

    public HomeViewModel(
        ISessionService sessionService,
        IAuthService authService,
        INavigationService navigationService)
    {
        _sessionService = sessionService;
        _authService = authService;
        _navigationService = navigationService;
        Title = "Inicio";
    }

    public async Task LoadAsync()
    {
        var session = _sessionService.CurrentSession ?? await _sessionService.LoadAsync();
        var usuario = session?.Usuario;

        if (usuario is null)
        {
            return;
        }

        Username = usuario.Username ?? string.Empty;
        TipoUsuario = usuario.TipoUsuario ?? string.Empty;
        Sede = BuildSede(usuario);
    }

    [RelayCommand]
    private async Task CerrarSesionAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            try
            {
                await _authService.LogoutAsync();
            }
            catch (Exception)
            {
                // El logout remoto puede fallar; la sesión local se borra igualmente.
            }

            try
            {
                await _sessionService.ClearAsync();
            }
            catch (Exception)
            {
            }

            await _navigationService.NavigateToLoginAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string BuildSede(Usuario usuario)
    {
        if (usuario.Cedis is not null)
        {
            return $"CEDIS: {usuario.Cedis.NombreCedis}";
        }

        if (usuario.Tienda is not null)
        {
            return $"Tienda: {usuario.Tienda.NombreTienda}";
        }

        return "Sin sede asignada";
    }
}
