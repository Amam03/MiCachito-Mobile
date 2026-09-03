using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Navigation;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Seleccionar Ciudad" (pantalla 8).
/// Aparece tras seleccionar un sorteo activo (pantalla 7.x) y muestra
/// las ciudades/CEDIS donde se puede vender ese sorteo.
///
/// Recibe el Id del sorteo y el Id del tipo de sorteo vía QueryProperty
/// (navegación Shell), para mantener el contexto del flujo de venta.
///
/// REGLA DE NEGOCIO: "CUALQUIER CIUDAD" siempre es el primer elemento
/// de la lista; después van las ciudades/CEDIS correspondientes.
/// </summary>
[QueryProperty(nameof(SorteoIdStr), "sorteoId")]
[QueryProperty(nameof(TipoSorteoIdStr), "tipoSorteoId")]
public partial class SeleccionCiudadViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<CiudadCedis> _ciudades = new();

    private int? _sorteoId;
    private int? _tipoSorteoId;

    /// <summary>
    /// Id del sorteo activo seleccionado en la pantalla 7.x,
    /// recibido vía navegación Shell (como string).
    /// </summary>
    public string? SorteoIdStr
    {
        get => _sorteoId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _sorteoId = id;
            }
        }
    }

    /// <summary>
    /// Id del tipo de sorteo (contexto de navegación desde pantalla 6),
    /// recibido vía navegación Shell (como string).
    /// </summary>
    public string? TipoSorteoIdStr
    {
        get => _tipoSorteoId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _tipoSorteoId = id;
            }
        }
    }

    public SeleccionCiudadViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Title = "Seleccionar Ciudad";
        CargarCiudades();
    }

    /// <summary>
    /// Carga las ciudades desde el catálogo mock ("CUALQUIER CIUDAD" primera).
    /// Futuro: reemplazar por llamada a GET /api/cedis (con sede-scoping).
    /// </summary>
    private void CargarCiudades()
    {
        Ciudades = new ObservableCollection<CiudadCedis>(CiudadesCedisData.ObtenerTodas());
    }

    /// <summary>
    /// Selección de una ciudad/CEDIS para el sorteo en curso: navega a la
    /// pantalla "Agregar Boletos" (pantallas 9.1 / 9.2) pasando el contexto
    /// completo del flujo (sorteo + tipo + ciudad).
    /// "Cualquier ciudad" (IdCiudad = 0) muestra todas las tiendas.
    /// </summary>
    [RelayCommand]
    private Task SeleccionarCiudadAsync(CiudadCedis? ciudad)
    {
        if (IsBusy || ciudad is null)
        {
            return Task.CompletedTask;
        }

        return _navigationService.NavigateToAgregarBoletosAsync(
            _sorteoId ?? 0,
            _tipoSorteoId ?? 0,
            ciudad.IdCiudad);
    }

    /// <summary>
    /// Retrocede a la pantalla anterior (sorteos activos).
    /// </summary>
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
