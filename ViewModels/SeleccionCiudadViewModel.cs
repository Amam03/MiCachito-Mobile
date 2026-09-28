using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Navigation;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Seleccionar Ciudad" (pantalla 8).
/// Aparece tras seleccionar una dotación (pantalla 7.x) y muestra
/// las ciudades/CEDIS donde se puede vender ese sorteo.
///
/// Recibe la CLAVE de la dotación (id_sorteo|numero_sorteo, ej.
/// "41|4024") como string vía QueryProperty y el Id del tipo de
/// sorteo, para mantener el contexto del flujo de venta. La clave
/// viaja intacta a la pantalla 9.x (Agregar Boletos).
///
/// REGLA DE NEGOCIO: "CUALQUIER CIUDAD" siempre es el primer elemento
/// de la lista; después van las ciudades/CEDIS correspondientes.
/// La ciudad es solo un PICKER operativo (P3 pendiente; ciudad fuera
/// del MVP: la venta F3 no la persiste).
/// </summary>
[QueryProperty(nameof(SorteoIdStr), "sorteoId")]
[QueryProperty(nameof(TipoSorteoIdStr), "tipoSorteoId")]
public partial class SeleccionCiudadViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<CiudadCedis> _ciudades = new();

    private string? _sorteoClave;
    private int? _tipoSorteoId;

    /// <summary>
    /// CLAVE de la dotación seleccionada en la pantalla 7.x
    /// ("41|4024"), recibida vía navegación Shell (como string).
    /// </summary>
    public string? SorteoIdStr
    {
        get => _sorteoClave;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _sorteoClave = value;
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
    /// Pendiente P3: catálogo real de ciudades (la venta no persiste ciudad).
    /// </summary>
    private void CargarCiudades()
    {
        Ciudades = new ObservableCollection<CiudadCedis>(CiudadesCedisData.ObtenerTodas());
    }

    /// <summary>
    /// Selección de una ciudad/CEDIS para la dotación en curso: navega a la
    /// pantalla "Agregar Boletos" (pantallas 9.1 / 9.2) pasando la clave de
    /// la dotación + el contexto completo del flujo (tipo + ciudad).
    /// "Cualquier ciudad" (IdCiudad = 0) muestra todas las series.
    /// </summary>
    [RelayCommand]
    private Task SeleccionarCiudadAsync(CiudadCedis? ciudad)
    {
        if (IsBusy || ciudad is null)
        {
            return Task.CompletedTask;
        }

        return _navigationService.NavigateToAgregarBoletosAsync(
            _sorteoClave ?? string.Empty,
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
