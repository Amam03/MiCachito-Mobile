using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Navigation;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla de sorteos activos (vigentes, no celebrados)
/// del tipo de LOTENAL seleccionado.
/// Recibe el Id del tipo de sorteo vía QueryProperty (navegación Shell).
///
/// Regla de disponibilidad: un sorteo se muestra solo si su fecha de
/// celebración no ha pasado (FechaCelebracion >= hoy).
/// Equivale al scope pendientes() del backend:
///   vigente=1 AND celebrado=0 AND fecha_sorteo >= today
/// </summary>
[QueryProperty(nameof(TipoSorteoIdStr), "tipoSorteoId")]
public partial class SorteosActivosViewModel : BaseViewModel
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableCollection<SorteoActivoLotenal> _sorteosActivos = new();

    /// <summary>
    /// Id del tipo de sorteo recibido vía navegación Shell (como string).
    /// Al asignarse, carga los sorteos activos correspondientes.
    /// </summary>
    public string? TipoSorteoIdStr
    {
        get => _tipoSorteoId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                CargarSorteos(id);
            }
        }
    }

    private int? _tipoSorteoId;

    public SorteosActivosViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
        Title = "Numero de Sorteo";
    }

    /// <summary>
    /// Carga los sorteos desde el catálogo y filtra los disponibles
    /// (fecha de celebración >= hoy).
    /// Futuro: reemplazar por llamada a GET /api/sorteos?vigente=1
    /// que ya devuelve solo los pendientes desde el backend.
    /// </summary>
    private void CargarSorteos(int tipoId)
    {
        _tipoSorteoId = tipoId;

        var tipo = SorteosLotenalData.Sorteos.FirstOrDefault(s => s.Id == tipoId);
        if (tipo is not null)
        {
            Title = tipo.Nombre;
        }

        // Obtener todos los sorteos del tipo y filtrar solo los disponibles
        var disponibles = SorteosActivosLotenalData
            .ObtenerPorTipoId(tipoId)
            .Where(s => s.EstaDisponible)
            .ToList();

        SorteosActivos = new ObservableCollection<SorteoActivoLotenal>(disponibles);
    }

    /// <summary>
    /// Selección de un sorteo activo: navega a la pantalla
    /// "Seleccionar Ciudad" (pantalla 8) pasando el contexto del sorteo
    /// y del tipo. UI-only: sin lógica de venta por ahora.
    /// </summary>
    [RelayCommand]
    private Task SeleccionarSorteoActivoAsync(SorteoActivoLotenal? sorteo)
    {
        if (IsBusy || sorteo is null)
        {
            return Task.CompletedTask;
        }

        return _navigationService.NavigateToSeleccionCiudadAsync(
            sorteo.IdSorteo,
            _tipoSorteoId ?? 0);
    }

    /// <summary>
    /// Retrocede a la pantalla anterior (lista de tipos de sorteo).
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
