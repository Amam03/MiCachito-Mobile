using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Navigation;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla de selección de proveedor de Tiempo Aire.
/// Los proveedores provienen de Data/TiempoAireData.cs (estático, UI-only).
/// Al seleccionar un proveedor, navega a MontosTiempoAirePage con su Id.
/// </summary>
public partial class TiempoAireViewModel : BaseViewModel
{
    private readonly INavigationService _navigation;

    [ObservableProperty]
    private ObservableCollection<ProveedorTiempoAire> _proveedores = new();

    public TiempoAireViewModel(INavigationService navigation)
    {
        _navigation = navigation;
        Title = "Tiempo Aire";
        CargarProveedores();
    }

    private void CargarProveedores()
    {
        // Los 34 proveedores (con sus montos) provienen de TiempoAireData.
        // TODO: Reemplazar con datos del backend cuando exista el endpoint.
        Proveedores = new ObservableCollection<ProveedorTiempoAire>(TiempoAireData.Proveedores);
    }

    /// <summary>
    /// Al seleccionar un proveedor, navega a su pantalla de montos.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarProveedorAsync(ProveedorTiempoAire proveedor)
    {
        if (proveedor is null)
        {
            return;
        }

        await _navigation.NavigateToMontosTiempoAireAsync(proveedor.Id);
    }
}
