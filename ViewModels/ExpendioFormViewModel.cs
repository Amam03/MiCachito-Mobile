using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Navigation;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM del formulario de Expendio (mockups 5/6): modo Actualizar SOLO
/// Permisos de Venta (Sorteos Tec / Tiempo Aire). La identidad del expendio
/// (usuario/titular/domicilio) la administra Desktop (cont-cedis/clientes):
/// aquí se muestra en SOLO LECTURA. No hay modo Crear desde Mobile.
///
/// Los permisos son COMPARTIDOS a nivel billetero (billeteros.
/// tiene_prod_digitales / tiene_tiempo_aire): el formulario muestra el
/// estado del conjunto y Guardar lo envía por PUT
/// api/mobile/expendios/{id}/permisos; la respuesta actualiza la sesión y
/// la pestaña Vender refleja los botones al volver (OnAppearing).
///
/// Lotería Nacional NO tiene checkbox: sin flag backend, siempre visible
/// en Vender (decisión 2026-09-14, sin tocar Lotenal).
/// </summary>
[QueryProperty(nameof(ExpendioIdStr), "expendioId")]
[QueryProperty(nameof(ModoStr), "modo")]
public partial class ExpendioFormViewModel : BaseViewModel
{
    /// <summary>Expendio (credencial) en edición, del conjunto de la sesión.</summary>
    private ExpendioItem? _expendio;

    private readonly IExpendiosService _expendiosService;
    private readonly ISessionService _sessionService;
    private readonly INavigationService _navigationService;

    /// <summary>Título del header ("Actualizar Expendio").</summary>
    [ObservableProperty]
    private string _titulo = "Actualizar Expendio";

    /// <summary>Texto del botón verde final ("Actualizar").</summary>
    [ObservableProperty]
    private string _textoBoton = "Actualizar";

    /// <summary>True mientras viaja el PUT (deshabilita el botón).</summary>
    [ObservableProperty]
    private bool _guardando;

    // ── Identidad del expendio (SOLO LECTURA; la administra Desktop) ──

    [ObservableProperty]
    private string _usuario = string.Empty;

    [ObservableProperty]
    private string _titular = string.Empty;

    [ObservableProperty]
    private string _domicilio = string.Empty;

    // ── Permisos de Venta (compartidos a nivel billetero) ──

    /// <summary>Permiso Sorteos Tec (tiene_prod_digitales).</summary>
    [ObservableProperty]
    private bool _sorteosTec;

    /// <summary>Permiso Tiempo Aire (tiene_tiempo_aire).</summary>
    [ObservableProperty]
    private bool _tiempoAire;

    /// <summary>Permiso Lotería Nacional (tiene_lotenal).</summary>
    [ObservableProperty]
    private bool _lotenal;

    /// <summary>Negaciones (checkboxes vacíos, sin converters).</summary>
    public bool NoSorteosTec => !SorteosTec;
    public bool NoTiempoAire => !TiempoAire;
    public bool NoLotenal => !Lotenal;

    partial void OnSorteosTecChanged(bool value) => OnPropertyChanged(nameof(NoSorteosTec));
    partial void OnTiempoAireChanged(bool value) => OnPropertyChanged(nameof(NoTiempoAire));
    partial void OnLotenalChanged(bool value) => OnPropertyChanged(nameof(NoLotenal));

    /// <summary>True si el botón Actualizar está habilitado.</summary>
    public bool PuedeGuardar => !Guardando;

    /// <summary>Opacidad del botón Actualizar (0.5 mientras viaja el PUT).</summary>
    public float OpacidadGuardar => PuedeGuardar ? 1f : 0.5f;

    partial void OnGuardandoChanged(bool value)
    {
        OnPropertyChanged(nameof(PuedeGuardar));
        OnPropertyChanged(nameof(OpacidadGuardar));
    }

    public ExpendioFormViewModel(
        IExpendiosService expendiosService,
        ISessionService sessionService,
        INavigationService navigationService)
    {
        _expendiosService = expendiosService;
        _sessionService = sessionService;
        _navigationService = navigationService;
        Title = "Expendio";
    }

    /// <summary>Id del expendio recibido vía navegación Shell.</summary>
    public string? ExpendioIdStr { get; set; }

    /// <summary>Modo recibido vía navegación Shell ("actualizar").</summary>
    public string? ModoStr { get; set; }

    /// <summary>
    /// Carga inicial: resuelve el expendio de la sesión (por id de navegación)
    /// y fija los checkboxes desde el estado COMPARTIDO del billetero. El
    /// formulario se abre desde cualquier tarjeta del conjunto; el estado
    /// mostrado es el mismo (permisos por billetero, no por expendio).
    /// Llamado desde OnAppearing (patrón [QueryProperty] del proyecto).
    /// </summary>
    public void AlAparecer()
    {
        int id = int.TryParse(ExpendioIdStr, out id) ? id : 0;

        _expendio = null;
        foreach (ExpendioItem e in ExpendiosViewModel.ExpendiosCompartidos)
        {
            if (e.IdExpendio == id)
            {
                _expendio = e;
                break;
            }
        }

        // Estado compartido de permisos: siempre desde el billetero de la
        // sesión (refrescado por el GET de la pestaña Expendios).
        var billetero = _sessionService.CurrentSession?.Billetero;
        SorteosTec = billetero?.TieneProdDigitales == 1;
        TiempoAire = billetero?.TieneTiempoAire == 1;
        Lotenal = billetero?.TieneLotenal == 1;

        Usuario = _expendio?.Usuario ?? string.Empty;
        Titular = _expendio?.Titular ?? string.Empty;
        Domicilio = _expendio?.Domicilio ?? string.Empty;

        OnPropertyChanged(nameof(Usuario));
        OnPropertyChanged(nameof(Titular));
        OnPropertyChanged(nameof(Domicilio));
        OnPropertyChanged(nameof(NoSorteosTec));
        OnPropertyChanged(nameof(NoTiempoAire));
        OnPropertyChanged(nameof(NoLotenal));

        Titulo = "Actualizar Expendio";
        TextoBoton = "Actualizar";
        Title = Titulo;
    }

    /// <summary>Alterna el permiso Sorteos Tec (checkbox).</summary>
    [RelayCommand]
    private void AlternarSorteosTec() => SorteosTec = !SorteosTec;

    /// <summary>Alterna el permiso Tiempo Aire (checkbox).</summary>
    [RelayCommand]
    private void AlternarTiempoAire() => TiempoAire = !TiempoAire;

    /// <summary>Alterna el permiso Lotenal (checkbox).</summary>
    [RelayCommand]
    private void AlternarLotenal() => Lotenal = !Lotenal;

    /// <summary>
    /// Guardar: PUT api/mobile/expendios/{id}/permisos con los flags del
    /// formulario. La respuesta trae el billetero actualizado: se escribe
    /// en la sesión (SessionService) para que la pestaña Vender refleje
    /// los botones al volver SIN reiniciar la app. Errores: 401 → Login
    /// (patrón Splash), otros → alerta sin perder la captura.
    /// </summary>
    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Guardando || _expendio is null)
        {
            return;
        }

        Guardando = true;
        try
        {
            var respuesta = await _expendiosService.ActualizarPermisosAsync(
                _expendio.IdExpendio,
                SorteosTec ? 1 : 0,
                TiempoAire ? 1 : 0,
                Lotenal ? 1 : 0);

            if (respuesta?.Billetero is not null)
            {
                await _sessionService.UpdateBilleteroAsync(respuesta.Billetero);
            }

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (ApiException ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await _sessionService.ClearAsync();
                await _navigationService.NavigateToLoginAsync();
                return;
            }
            await AlertarAsync(ex.ServerMessage ?? "No fue posible actualizar los permisos.");
        }
        catch (Exception)
        {
            await AlertarAsync("No fue posible conectar con el servidor.");
        }
        finally
        {
            Guardando = false;
            OnPropertyChanged(nameof(PuedeGuardar));
        }
    }

    /// <summary>Volver sin cambios (flecha del header).</summary>
    [RelayCommand]
    private async Task CancelarAsync()
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    private static async Task AlertarAsync(string mensaje)
    {
        if (Application.Current is not null && Application.Current.Windows.Count > 0)
        {
            await Application.Current.Windows[0].Page!.DisplayAlertAsync("Expendios", mensaje, "OK");
        }
    }
}
