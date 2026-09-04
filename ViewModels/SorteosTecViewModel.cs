using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Seleccionar Sorteo" Tec (pantalla 12),
/// a la que se llega desde el botón "Sorteos Tec" del Home.
///
/// Tarjetas con SOLO el nombre del sorteo + precio (decisión del
/// usuario, sep-2026: sin No. ni fecha). Al seleccionar una tarjeta
/// se navega a SeleccionarBilletePage (pantalla 13) pasando sorteoId.
///
/// Badge del carrito: número de billetes agregados en TODOS los
/// sorteos (catálogo completo, no la lista filtrada).
/// </summary>
public partial class SorteosTecViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<SorteoTec> _sorteos = new(SorteosTecData.ObtenerTodos());

    public SorteosTecViewModel()
    {
        Title = "Seleccionar Sorteo";
    }

    /// <summary>Badge del carrito flotante: billetes agregados.</summary>
    public string BadgeTexto => SorteosTecData.TotalAgregados().ToString();

    /// <summary>True si hay billetes en el carrito (muestra el badge).</summary>
    public bool HayEnCarrito => SorteosTecData.TotalAgregados() > 0;

    /// <summary>
    /// Al volver de la 13/14 (OnAppearing): refresca el badge.
    /// </summary>
    public void AlAparecer()
    {
        NotificarDerivadas();
    }

    /// <summary>
    /// Al tocar una tarjeta: navega a la pantalla de billetes del sorteo.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarSorteoAsync(SorteoTec? sorteo)
    {
        if (IsBusy || sorteo is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(Views.SeleccionarBilletePage)}?sorteoId={sorteo.IdSorteo}");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Carrito flotante: abre el carrito Tec (pantalla 14).
    /// </summary>
    [RelayCommand]
    private async Task AbrirCarritoAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(nameof(Views.CarritoTecPage));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NotificarDerivadas()
    {
        OnPropertyChanged(nameof(BadgeTexto));
        OnPropertyChanged(nameof(HayEnCarrito));
    }
}
