using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel del listado "Devoluciones" (mockup 6): registros guardados
/// en memoria (estado local; sin seeds) + estado vacío "No se encontraron
/// Devoluciones" + FAB "+" hacia la Lista de Sorteos. Fase solo-interfaz.
/// </summary>
public partial class DevolucionesViewModel : BaseViewModel
{
    private readonly DevolucionService _devoluciones;

    /// <summary>Registros guardados (más reciente primero).</summary>
    public ObservableCollection<Devolucion> Devoluciones { get; } = new();

    /// <summary>True cuando no hay registros (mensaje del mockup 6).</summary>
    [ObservableProperty]
    private bool sinRegistros = true;

    public DevolucionesViewModel(DevolucionService devoluciones)
    {
        _devoluciones = devoluciones;
        Title = "Devoluciones";
    }

    /// <summary>Refresca la lista al aparecer (tras Guardar en 6.2).</summary>
    public void AlAparecer()
    {
        Devoluciones.Clear();
        foreach (Devolucion d in _devoluciones.Devoluciones)
        {
            Devoluciones.Add(d);
        }

        SinRegistros = Devoluciones.Count == 0;
    }

    /// <summary>FAB "+": abre la Lista de Sorteos (mockup 6.1).</summary>
    [RelayCommand]
    private Task AgregarAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(nameof(Views.ListaSorteosDevolucionPage));
    }

    /// <summary>Regresa al menú de Gestión.</summary>
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
