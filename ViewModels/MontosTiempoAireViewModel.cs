using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Navigation;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla de selección de monto para un proveedor de Tiempo Aire.
/// Recibe el Id del proveedor vía QueryProperty (navegación Shell).
/// Al seleccionar un monto, navega a la pantalla de número telefónico.
/// </summary>
[QueryProperty(nameof(ProveedorIdStr), "proveedorId")]
public partial class MontosTiempoAireViewModel : BaseViewModel
{
    private readonly INavigationService _navigation;
    private ProveedorTiempoAire? _proveedor;

    [ObservableProperty]
    private ObservableCollection<MontoTiempoAire> _montos = new();

    [ObservableProperty]
    private string _nombreProveedor = string.Empty;

    [ObservableProperty]
    private Color _colorHeader = Color.FromArgb("#3A21DC");

    [ObservableProperty]
    private Color _textColorHeader = Colors.White;

    public MontosTiempoAireViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    /// <summary>
    /// Id del proveedor recibido vía navegación Shell (como string).
    /// Al asignarse, carga los datos del proveedor.
    /// </summary>
    public string? ProveedorIdStr
    {
        get => _proveedor?.Id.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                CargarProveedor(id);
            }
        }
    }

    /// <summary>
    /// Carga los datos del proveedor desde TiempoAireData.
    /// </summary>
    private void CargarProveedor(int id)
    {
        _proveedor = TiempoAireData.ObtenerPorId(id);
        if (_proveedor is null)
        {
            return;
        }

        NombreProveedor = _proveedor.DisplayText;
        ColorHeader = _proveedor.ColorTarjeta;
        TextColorHeader = _proveedor.TextColor;
        Title = NombreProveedor;

        var listaMontos = _proveedor.Montos
            .Select(m => new MontoTiempoAire { Valor = m })
            .ToList();

        Montos = new ObservableCollection<MontoTiempoAire>(listaMontos);
    }

    /// <summary>
    /// Selección del monto: marca visualmente y navega a la pantalla de número telefónico.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarMontoAsync(MontoTiempoAire? monto)
    {
        if (monto is null || _proveedor is null)
        {
            return;
        }

        // Marca visual (deselecciona anteriores, selecciona el actual)
        foreach (var m in Montos)
        {
            m.IsSelected = false;
        }
        monto.IsSelected = true;

        // Navega a la pantalla de número telefónico con el proveedor y monto
        await _navigation.NavigateToNumeroTelefonoTiempoAireAsync(
            _proveedor.Id, monto.Valor);
    }

    /// <summary>
    /// Retrocede a la pantalla anterior (lista de proveedores).
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
