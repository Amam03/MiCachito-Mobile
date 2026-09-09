using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel del listado "Depósitos" (mockup 7): registros guardados en
/// memoria + FAB "+" hacia Nuevo Depósito. Fase solo-interfaz.
/// </summary>
public partial class DepositosViewModel : BaseViewModel
{
    private readonly DepositoService _depositos;

    /// <summary>Registros guardados (más reciente primero).</summary>
    public ObservableCollection<Deposito> Registros { get; } = new();

    /// <summary>True cuando no hay registros (pantalla vacía del mockup 7).</summary>
    [ObservableProperty]
    private bool sinRegistros = true;

    public DepositosViewModel(DepositoService depositos)
    {
        _depositos = depositos;
        Title = "Depósitos";
    }

    /// <summary>Refresca la lista al aparecer (tras Guardar en 7.1/7.2).</summary>
    public void AlAparecer()
    {
        Registros.Clear();
        foreach (Deposito d in _depositos.Depositos)
        {
            Registros.Add(d);
        }

        SinRegistros = Registros.Count == 0;
    }

    /// <summary>FAB "+": abre Nuevo Depósito (mockup 7.1).</summary>
    [RelayCommand]
    private Task AgregarAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(nameof(Views.NuevoDepositoPage));
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
