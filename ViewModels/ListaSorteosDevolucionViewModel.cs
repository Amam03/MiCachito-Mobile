using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la "Lista de Sorteos" de Devolución (mockup 6.1): las
/// DOTACIONES con material LIVE asignado al billetero de la sesión (GET
/// api/mobile/ventas/sorteos — misma fuente que Vender 7.x; sin regla
/// artificial de fechas). Al seleccionar una dotación inicia la
/// devolución (contexto de dotación para desambiguar los boletos
/// escaneados: la combinación numero_billete+serie+fraccion se repite
/// entre dotaciones 4024/4025) y abre 6.2.
///
/// El mock SorteosActivosLotenalData queda SIN consumidores (deprecado
/// como archivo, no se borra — regla §E).
///
/// Estados: overlay "Procesando" al cargar, aviso rojo con reentrar
/// (reintentar), vacío "No hay sorteos disponibles" (patrón Gestión/F2).
/// </summary>
public partial class ListaSorteosDevolucionViewModel : BaseViewModel
{
    private readonly DevolucionService _devoluciones;
    private readonly MobileVentasService _ventas;

    /// <summary>Dotaciones con material LIVE del billetero (real).</summary>
    [ObservableProperty]
    private ObservableCollection<DotacionDisponible> sorteos = new();

    /// <summary>True mientras carga (overlay "Procesando").</summary>
    [ObservableProperty]
    private bool cargando;

    /// <summary>Mensaje de error de la carga (reintentable re-entrando).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True cuando hay error visible.</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>True cuando la lista cargó y NO hay dotaciones (vacío).</summary>
    [ObservableProperty]
    private bool sinSorteos;

    public ListaSorteosDevolucionViewModel(
        DevolucionService devoluciones,
        MobileVentasService ventas)
    {
        _devoluciones = devoluciones;
        _ventas = ventas;
        Title = "Lista de Sorteos";
    }

    /// <summary>Carga las dotaciones reales al aparecer (o reintentar).</summary>
    [RelayCommand]
    public async Task AlAparecerAsync()
    {
        if (Cargando)
        {
            return;
        }

        Cargando = true;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(HayError));
        try
        {
            IReadOnlyList<DotacionDisponible> lista =
                await _ventas.SorteosDisponiblesAsync().ConfigureAwait(true);

            Sorteos = new ObservableCollection<DotacionDisponible>(lista);
            SinSorteos = lista.Count == 0;
        }
        catch (TaskCanceledException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (OperationCanceledException)
        {
            MensajeError = "Consulta cancelada";
            OnPropertyChanged(nameof(HayError));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (ApiException ex)
        {
            MensajeError = ex.Message;
            OnPropertyChanged(nameof(HayError));
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Selecciona una dotación: inicia la devolución (contexto con
    /// id_sorteo + dotación para desambiguar) y abre Nueva Devolución.
    /// </summary>
    [RelayCommand]
    private Task SeleccionarSorteoAsync(DotacionDisponible? sorteo)
    {
        if (sorteo is null || Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        _devoluciones.Iniciar(sorteo);
        return Shell.Current.GoToAsync(nameof(Views.NuevaDevolucionPage));
    }

    /// <summary>Regresa al listado de devoluciones.</summary>
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
