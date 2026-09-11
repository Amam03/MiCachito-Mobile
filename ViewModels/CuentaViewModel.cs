using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Navigation;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// Pestaña Cuenta (mockups 1-6). Encabezado con saldo (patrón Gestión),
/// identidad de la app (logo + Mi Cachito + versión instalada), usuario
/// autenticado real (ISessionService) y filas Notificaciones / Impresora
/// (desplegable) / Cerrar Sesión.
/// Fase SOLO INTERFAZ: notificaciones sin datos reales (estado vacío,
/// sin registros inventados); dispositivos = Bluetooth vinculado real
/// vía IImpresoraService.
/// </summary>
public partial class CuentaViewModel : BaseViewModel
{
    private readonly ISessionService _sessionService;
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly IImpresoraService _impresoraService;

    [ObservableProperty]
    private string _saldo = "$0.00";

    [ObservableProperty]
    private string _nombreUsuario = string.Empty;

    [ObservableProperty]
    private string _versionApp = string.Empty;

    /// <summary>Sección Impresora desplegada (mockup 4).</summary>
    [ObservableProperty]
    private bool _impresoraExpandida;

    /// <summary>Negación derivada (patrón del proyecto: sin converters).</summary>
    public bool NoImpresoraExpandida => !ImpresoraExpandida;

    partial void OnImpresoraExpandidaChanged(bool value)
    {
        OnPropertyChanged(nameof(NoImpresoraExpandida));
    }

    /// <summary>Modal de confirmación de Cerrar Sesión (mockup 6).</summary>
    [ObservableProperty]
    private bool _modalCerrarSesionVisible;

    /// <summary>Diálogo Atención: sin impresora enlazada (mockup 2).</summary>
    [ObservableProperty]
    private bool _dialogoSinImpresoraVisible;

    /// <summary>Dispositivos Bluetooth vinculados (mockup 5).</summary>
    public ObservableCollection<string> DispositivosEnlazados { get; } = new();

    /// <summary>True cuando NO hay dispositivos (muestra diálogo Atención).</summary>
    [ObservableProperty]
    private bool _sinDispositivos;

    /// <summary>
    /// Marca que el aviso "sin impresora" ya se mostró en esta sesión de
    /// la pestaña (evita repetirlo en cada expansión; mockup 4 despliega
    /// sin diálogo tras el primer aviso).
    /// </summary>
    private bool _avisoSinImpresoraMostrado;

    public CuentaViewModel(
        ISessionService sessionService,
        IAuthService authService,
        INavigationService navigationService,
        IImpresoraService impresoraService)
    {
        _sessionService = sessionService;
        _authService = authService;
        _navigationService = navigationService;
        _impresoraService = impresoraService;
    }

    /// <summary>
    /// Carga identidad al aparecer (patrón AlAparecer del proyecto).
    /// La versión sale de AppInfo (real, no hardcodeada).
    /// </summary>
    public async Task AlAparecerAsync()
    {
        // Versión instalada (fuente real del SO)
        VersionApp = $"v{VersionTracking.CurrentVersion}";

        // Usuario autenticado real
        var session = _sessionService.CurrentSession ?? await _sessionService.LoadAsync();
        var usuario = session?.Usuario;
        NombreUsuario = usuario?.Username ?? string.Empty;
    }

    /// <summary>
    /// Expande/contrae la sección Impresora (mockup 4). Al expandir
    /// consulta los dispositivos Bluetooth vinculados reales; si NO hay
    /// ninguno muestra el diálogo Atención del mockup 2 SOLO la primera
    /// vez (las siguientes expansiones quedan sin diálogo, como el
    /// mockup 4; el diálogo no debe ser permanente ni repetitivo).
    /// </summary>
    [RelayCommand]
    private async Task AlternarImpresoraAsync()
    {
        ImpresoraExpandida = !ImpresoraExpandida;

        if (ImpresoraExpandida && !_avisoSinImpresoraMostrado)
        {
            DispositivosEnlazados.Clear();
            var dispositivos = await _impresoraService.ObtenerDispositivosEnlazadosAsync();
            foreach (var d in dispositivos)
            {
                DispositivosEnlazados.Add(d);
            }

            SinDispositivos = DispositivosEnlazados.Count == 0;
            if (SinDispositivos)
            {
                // Mockup 2: diálogo "Atención / No se ha enlazado ninguna
                // impresora" solo cuando corresponde (sin vínculos) y una
                // sola vez por sesión de la pestaña.
                DialogoSinImpresoraVisible = true;
                _avisoSinImpresoraMostrado = true;
            }
        }
    }

    /// <summary>Cierra el diálogo Atención (botón ACEPTAR).</summary>
    [RelayCommand]
    private void CerrarDialogoSinImpresora()
    {
        DialogoSinImpresoraVisible = false;
    }

    /// <summary>
    /// Abre la pantalla Dispositivos Enlazados (mockup 5). Ruta relativa
    /// registrada en AppShell.xaml.cs.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarDispositivoAsync()
    {
        await Shell.Current.GoToAsync(nameof(Views.DispositivosEnlazadosPage));
    }

    /// <summary>Notificaciones (mockup 3): pantalla propia, estado vacío.</summary>
    [RelayCommand]
    private async Task AbrirNotificacionesAsync()
    {
        await Shell.Current.GoToAsync(nameof(Views.NotificacionesPage));
    }

    /// <summary>Abre el modal de confirmación (mockup 6) atenuando el fondo.</summary>
    [RelayCommand]
    private void AbrirCerrarSesion()
    {
        ModalCerrarSesionVisible = true;
    }

    /// <summary>Cancelar: cierra el modal y permanece en Cuenta.</summary>
    [RelayCommand]
    private void CancelarCerrarSesion()
    {
        ModalCerrarSesionVisible = false;
    }

    /// <summary>
    /// Aceptar: finaliza la sesión (logout remoto best-effort + borrado
    /// local, mismo flujo que HomeViewModel.CerrarSesionAsync) y regresa
    /// a Login. NavigateToLoginAsync reemplaza la ventana: no es posible
    /// volver a las pantallas protegidas con el botón atrás.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmarCerrarSesionAsync()
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

            ModalCerrarSesionVisible = false;
            await _navigationService.NavigateToLoginAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
