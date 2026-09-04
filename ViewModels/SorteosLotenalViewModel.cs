using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Navigation;

namespace MiCachito.Mobile.ViewModels;

public partial class SorteosLotenalViewModel : BaseViewModel
{
    private readonly INavigationService _navigation;

    public ObservableCollection<SorteoLotenal> Sorteos { get; } = [];

    public SorteosLotenalViewModel(INavigationService navigation)
    {
        _navigation = navigation;
        Title = "LOTENAL";
        foreach (var sorteo in SorteosLotenalData.Sorteos)
        {
            Sorteos.Add(sorteo);
        }
    }

    /// <summary>
    /// Selección de un tipo de sorteo: navega a la pantalla de sorteos activos.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarSorteoAsync(SorteoLotenal sorteo)
    {
        if (IsBusy || sorteo is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await _navigation.NavigateToSorteosActivosAsync(sorteo.Id);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
