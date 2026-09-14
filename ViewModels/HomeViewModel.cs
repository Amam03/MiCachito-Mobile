using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    /// <summary>
    /// Saldo mostrado en el encabezado de la pestaña (patron Gestion).
    /// Inicial en $0.00: se calculara desde el backend cuando exista la
    /// conexion; por ahora no la hay (fase solo-interfaz).
    /// </summary>
    [ObservableProperty]
    private string saldo = "$0.00";

    // ── Visibilidad de botones por Permisos de Venta (regla spec Expendios §7/§14):
    // la fuente de verdad es el expendio, NO esta pantalla. Desde Fase 1 se leen
    // de la sesión mobile (datosBilletero del login/verify). ──

    /// <summary>True para mostrar el botón Sorteos Tec.</summary>
    [ObservableProperty]
    private bool muestraSorteosTec = true;

    /// <summary>True para mostrar el botón Tiempo Aire.</summary>
    [ObservableProperty]
    private bool muestraTiempoAire = true;

    /// <summary>True para mostrar el botón LOTENAL.</summary>
    [ObservableProperty]
    private bool muestraLotenal = true;

    public HomeViewModel(
        ISessionService sessionService,
        IAuthService authService,
        INavigationService navigationService)
    {
        _sessionService = sessionService;
        _authService = authService;
        _navigationService = navigationService;
        Title = "Vender";
    }

    public async Task LoadAsync()
    {
        var session = _sessionService.CurrentSession ?? await _sessionService.LoadAsync();
        var expendio = session?.Expendio;
        var billetero = session?.Billetero;

        if (expendio is null)
        {
            return;
        }

        Username = billetero?.NombreCompleto ?? expendio.Usuario ?? string.Empty;
        TipoUsuario = "Expendio";
        Sede = $"Expendio: {expendio.Usuario}";

        CargarPermisosVenta();
    }

    /// <summary>
    /// Botones de Vender según los Permisos de Venta del expendio autenticado
    /// (única fuente de verdad: la configuración hecha en Gestión/Expendios
    /// desde Desktop; llega en el login/verify mobile via datosBilletero()).
    /// Mapeo decidido con el usuario (2026-09-14):
    /// - Tiempo Aire -> tiene_tiempo_aire
    /// - Sorteos Tec -> tiene_prod_digitales
    /// - Lotenal -> habilitado por ahora (el backend NO tiene flag propio
    ///   para Lotenal; si se necesita activar/desactivar por expendio se
    ///   analizará como cambio separado, sin tocar tablas compartidas).
    /// </summary>
    private void CargarPermisosVenta()
    {
        var billetero = _sessionService.CurrentSession?.Billetero
            ?? _sessionService.LoadAsync().GetAwaiter().GetResult()?.Billetero;

        if (billetero is null)
        {
            MuestraSorteosTec = true;
            MuestraTiempoAire = true;
            MuestraLotenal = true;
            return;
        }

        MuestraSorteosTec = billetero.TieneProdDigitales == 1;
        MuestraTiempoAire = billetero.TieneTiempoAire == 1;
        MuestraLotenal = true;
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

    [RelayCommand]
    private async Task TiempoAireAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await _navigationService.NavigateToTiempoAireAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LotenalAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await _navigationService.NavigateToSorteosLotenalAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SorteosTecAsync()
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
                await Shell.Current.GoToAsync(nameof(Views.SorteosTecPage));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
