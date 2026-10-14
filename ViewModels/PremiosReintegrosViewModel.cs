using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la lista de movimientos (mockup 3: resumen + listado +
/// FAB "+"). Histórico REAL del billetero (GET api/mobile/premios-reintegros,
/// fecha descendente). Estados: cargando (overlay), vacío, error con
/// reintento por re-entrada (patrón Depósitos/Tickets).
/// </summary>
public partial class PremiosReintegrosViewModel : BaseViewModel
{
    private readonly PremiosReintegrosService _servicio;

    public ObservableCollection<MovimientoPremiosReintegros> Movimientos => _servicio.Movimientos;

    /// <summary>True mientras corre el GET del histórico (overlay).</summary>
    [ObservableProperty]
    private bool cargando;

    /// <summary>Mensaje de error de la carga (reintentar re-entrando).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    public bool EstaVacio => !Cargando && !HayError && Movimientos.Count == 0;

    [ObservableProperty]
    private string mensajeVacio = "No hay movimientos registrados";

    public PremiosReintegrosViewModel(PremiosReintegrosService servicio)
    {
        _servicio = servicio;
        Title = "Premios y Reintegros";
    }

    /// <summary>
    /// Carga el histórico real al entrar (y al volver de captura/detalle).
    /// </summary>
    public async Task AlAparecerAsync()
    {
        if (Cargando)
        {
            return;
        }

        Cargando = true;
        MensajeError = string.Empty;
        try
        {
            try
            {
                await _servicio.CargarMovimientosAsync();
            }
            catch (TaskCanceledException)
            {
                MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            }
            catch (OperationCanceledException)
            {
                MensajeError = "Consulta cancelada";
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            }
            catch (ApiException ex)
            {
                MensajeError = $"No se pudieron cargar los movimientos: {ex.Message}";
            }
        }
        finally
        {
            Cargando = false;
            // Derivadas al FINAL (con Cargando en false): notificarlas dentro
            // del try dejaba EstaVacio siempre en false (patrón Depósitos).
            OnPropertyChanged(nameof(Movimientos));
            OnPropertyChanged(nameof(TotalPremiosText));
            OnPropertyChanged(nameof(TotalReintegrosText));
            OnPropertyChanged(nameof(TotalGeneralText));
            OnPropertyChanged(nameof(HayError));
            OnPropertyChanged(nameof(EstaVacio));
        }
    }

    public string TotalPremiosText => Formato(_servicio.TotalPremios);

    public string TotalReintegrosText => Formato(_servicio.TotalReintegros);

    public string TotalGeneralText => Formato(_servicio.TotalGeneral);

    private static string Formato(decimal v) => $"${v:0.00}";

    /// <summary>Tap en un registro: abre su detalle (id por query param).</summary>
    [RelayCommand]
    private Task AbrirDetalleAsync(MovimientoPremiosReintegros movimiento)
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(
            $"{nameof(Views.DetallePremiosReintegrosPage)}?id={movimiento.IdMovimiento}");
    }

    /// <summary>FAB "+": abre la pantalla de captura (mockup 3.3).</summary>
    [RelayCommand]
    private Task AgregarAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(nameof(Views.CapturaPremiosReintegrosPage));
    }

    [RelayCommand]
    private Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }
        return Task.CompletedTask;
    }
}
