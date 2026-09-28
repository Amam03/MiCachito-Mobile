using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Navigation;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla de sorteos activos (7.x) del tipo de
/// LOTENAL seleccionado. F2 CONEXIÓN REAL: dotaciones de
/// GET api/mobile/ventas/sorteos — material LIVE realmente asignado
/// al billetero de la sesión (billetes_loteria estatus='asignado'),
/// filtradas por el producto del tipo elegido en la pantalla 6.
///
/// Recibe el Id del tipo de sorteo vía QueryProperty (pantalla 6).
/// Una fila por DOTACIÓN (id_sorteo + numero_sorteo): dos dotaciones
/// del mismo sorteo (MAYOR - 4024 / MAYOR - 4025) son DOS filas con
/// billetes separados. Material legacy sin número queda en la fila
/// "sin número" (misma convención que Gestión).
///
/// Sin regla artificial de fechas (§A.5): todo lo que el backend
/// lista tiene material asignado hoy — disponible por definición.
///
/// Estados (patrón Gestión Sorteos): overlay "Procesando" al cargar,
/// aviso rojo de error (reintentar re-entrando), estado vacío "Sin
/// sorteos disponibles" cuando el billetero no tiene material LIVE
/// del tipo elegido.
/// </summary>
[QueryProperty(nameof(TipoSorteoIdStr), "tipoSorteoId")]
public partial class SorteosActivosViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;
    private readonly MobileVentasService _servicio;

    [ObservableProperty]
    private ObservableCollection<DotacionDisponible> sorteosActivos = new();

    /// <summary>True mientras carga la lista al aparecer (overlay).</summary>
    [ObservableProperty]
    private bool cargandoLista;

    /// <summary>Mensaje de error de la carga (reintentable re-entrando).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True cuando hay error visible.</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>True cuando la lista cargó y NO hay dotaciones (estado vacío).</summary>
    [ObservableProperty]
    private bool sinSorteos;

    private int? _tipoSorteoId;
    private bool _cargado;

    /// <summary>
    /// Id del tipo de sorteo recibido vía navegación Shell (como string).
    /// Al asignarse dispara la carga REAL de dotaciones.
    /// </summary>
    public string? TipoSorteoIdStr
    {
        get => _tipoSorteoId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _tipoSorteoId = id;
                IntentarCargar();
            }
        }
    }

    public SorteosActivosViewModel(
        INavigationService navigationService,
        MobileVentasService servicio)
    {
        _navigationService = navigationService;
        _servicio = servicio;
        Title = "Numero de Sorteo";
    }

    /// <summary>
    /// Carga las dotaciones REALES del tipo elegido al recibir el
    /// parámetro de navegación. El tipo mapea el nombre del producto
    /// de la pantalla 6 (SorteosLotenalData): el backend devuelve
    /// TODAS las dotaciones LN del billetero; el filtro por producto
    /// es local (el endpoint es por categoría LN completa).
    /// </summary>
    private void IntentarCargar()
    {
        if (_cargado || _tipoSorteoId is null)
        {
            return;
        }

        _cargado = true;
        _ = CargarAsync(_tipoSorteoId.Value);
    }

    /// <summary>
    /// Carga real: dotaciones del billetero filtradas por el producto
    /// del tipo elegido (pantalla 6). Filas sin producto no se
    /// inventan: el filtro deja solo las del tipo (o todas si el tipo
    /// no mapea a producto — falla segura a "mostrar material").
    /// </summary>
    private async Task CargarAsync(int tipoId)
    {
        if (CargandoLista)
        {
            return;
        }

        CargandoLista = true;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(HayError));
        try
        {
            // Nombre del producto que representa el tipo elegido
            // (pantalla 6): "SORTEO MAYOR" → "MAYOR".
            var tipo = Data.SorteosLotenalData.Sorteos.FirstOrDefault(s => s.Id == tipoId);
            string? nombreProducto = tipo is null
                ? null
                : tipo.Nombre.StartsWith("SORTEO ", StringComparison.OrdinalIgnoreCase)
                    ? tipo.Nombre["SORTEO ".Length..]
                    : tipo.Nombre;

            IReadOnlyList<DotacionDisponible> lista =
                await _servicio.SorteosDisponiblesAsync().ConfigureAwait(true);

            IEnumerable<DotacionDisponible> filtradas = string.IsNullOrEmpty(nombreProducto)
                ? lista
                : lista.Where(d => string.Equals(
                    d.NombreProducto,
                    nombreProducto,
                    StringComparison.OrdinalIgnoreCase));

            SorteosActivos = new ObservableCollection<DotacionDisponible>(filtradas);
            SinSorteos = SorteosActivos.Count == 0;
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
            CargandoLista = false;
        }
    }

    /// <summary>
    /// Reintento manual tras error (aviso rojo): recarga las
    /// dotaciones del tipo en curso.
    /// </summary>
    [RelayCommand]
    private Task ReintentarAsync() => _tipoSorteoId is null
        ? Task.CompletedTask
        : CargarAsync(_tipoSorteoId.Value);

    /// <summary>
    /// Selección de una dotación: navega a "Seleccionar Ciudad"
    /// (pantalla 8) pasando el contexto del sorteo y del tipo. La
    /// pantalla 8 sigue siendo el picker operativo de ciudades (P3
    /// pendiente; ciudad fuera del MVP) y "Cualquier ciudad" lleva a
    /// la lista real de series (9.x).
    /// </summary>
    [RelayCommand]
    private Task SeleccionarSorteoActivoAsync(DotacionDisponible? sorteo)
    {
        if (IsBusy || sorteo is null)
        {
            return Task.CompletedTask;
        }

        // Clave de la dotación para las pantallas siguientes (9.x):
        // "41|4024" (id_sorteo + numero de dotación). "41|" = fila
        // legacy sin número.
        string clave = sorteo.IdSorteo + "|" + (sorteo.NumeroSorteo ?? string.Empty);
        return _navigationService.NavigateToSeleccionCiudadAsync(clave, _tipoSorteoId ?? 0);
    }

    /// <summary>Retrocede a la pantalla anterior (lista de tipos).</summary>
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
