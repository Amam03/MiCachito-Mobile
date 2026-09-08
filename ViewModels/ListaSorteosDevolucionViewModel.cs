using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la "Lista de Sorteos" de Devolución (mockup 6.1): sorteos
/// activos desde la misma fuente de LOTENAL (SorteosActivosLotenalData),
/// sin hardcodear. Al seleccionar inicia la devolución y abre 6.2.
/// Fase solo-interfaz.
/// </summary>
public partial class ListaSorteosDevolucionViewModel : BaseViewModel
{
    private readonly DevolucionService _devoluciones;

    /// <summary>Sorteos activos del catálogo LOTENAL (misma fuente ya preparada).</summary>
    public ObservableCollection<SorteoActivoLotenal> Sorteos { get; } = new();

    /// <summary>True mientras no haya sorteos disponibles (catálogo vacío).</summary>
    [ObservableProperty]
    private bool sinSorteos;

    public ListaSorteosDevolucionViewModel(DevolucionService devoluciones)
    {
        _devoluciones = devoluciones;
        Title = "Lista de Sorteos";
    }

    /// <summary>Carga los sorteos activos al aparecer.</summary>
    public void AlAparecer()
    {
        Sorteos.Clear();
        foreach (SorteoActivoLotenal s in Data.SorteosActivosLotenalData.ObtenerTodos()
                     .Where(s => s.EstaDisponible))
        {
            Sorteos.Add(s);
        }

        SinSorteos = Sorteos.Count == 0;
    }

    /// <summary>Selecciona un sorteo: inicia la devolución y abre Nueva Devolución.</summary>
    [RelayCommand]
    private Task SeleccionarSorteoAsync(SorteoActivoLotenal sorteo)
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
