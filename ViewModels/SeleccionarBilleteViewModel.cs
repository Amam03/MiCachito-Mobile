using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Seleccionar Billete" Tec (pantalla 13),
/// a la que se llega tras seleccionar un sorteo (pantalla 12).
///
/// Filas de billete (número + botón de carrito). Al tocar el carrito:
/// pleca verde en la fila (= agregado) y el badge del header suma
/// billetes. El carrito del header abre la pantalla 14.
///
/// El estado Agregado vive en las instancias compartidas del catálogo
/// (SorteosTecData): se refleja en la 12/14 sin recargarlas.
/// </summary>
[QueryProperty(nameof(SorteoIdStr), "sorteoId")]
public partial class SeleccionarBilleteViewModel : BaseViewModel
{
    /// <summary>
    /// Ignora toggles de fila durante la transición de entrada (defensa
    /// contra el tap fantasma de Android; mismo patrón de la 9.x).
    /// </summary>
    private const int MsIgnorarToggleTrasCarga = 600;

    private DateTime _cargadoEnUtc = DateTime.MinValue;
    private int? _sorteoId;
    private bool _cargado;

    [ObservableProperty]
    private ObservableCollection<BilleteTec> _billetes = new();

    [ObservableProperty]
    private string _nombrePantalla = string.Empty;

    /// <summary>
    /// Id del sorteo seleccionado, recibido como string vía navegación Shell.
    /// </summary>
    public string? SorteoIdStr
    {
        get => _sorteoId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _sorteoId = id;
                IntentarCargar();
            }
        }
    }

    public SeleccionarBilleteViewModel()
    {
        Title = "Seleccionar Billete";
    }

    private SorteoTec? Sorteo => _sorteoId is null
        ? null
        : SorteosTecData.ObtenerPorId(_sorteoId.Value);

    // ============ Derivados (notificar con NotificarDerivadas) ============

    /// <summary>Badge del carrito del header: billetes agregados (todos los sorteos).</summary>
    public string BadgeTexto => SorteosTecData.TotalAgregados().ToString();

    /// <summary>True con al menos un billete agregado (muestra el badge).</summary>
    public bool HayEnCarrito => SorteosTecData.TotalAgregados() > 0;

    /// <summary>True sin billetes en el sorteo (mensaje "Sin billetes disponibles").</summary>
    public bool SinBilletes => Billetes.Count == 0;

    /// <summary>
    /// Carga la lista solo cuando el parámetro de navegación está disponible.
    /// </summary>
    private void IntentarCargar()
    {
        if (_cargado || _sorteoId is null)
        {
            return;
        }

        _cargado = true;
        var sorteo = Sorteo;

        if (sorteo is null)
        {
            return;
        }

        NombrePantalla = sorteo.NombreSorteo;
        Title = sorteo.NombreSorteo;
        Billetes = new ObservableCollection<BilleteTec>(sorteo.Billetes);
        _cargadoEnUtc = DateTime.UtcNow;
        NotificarDerivadas();
    }

    /// <summary>
    /// Al volver de la 14 (OnAppearing): refresca el badge (Eliminar
    /// pudo quitar billetes).
    /// </summary>
    public void AlAparecer()
    {
        NotificarDerivadas();
    }

    /// <summary>
    /// Botón de carrito de una fila: agrega el billete (pleca verde) o
    /// lo quita si ya estaba agregado. Ignora el toque durante la
    /// ventana anti tap-fantasma tras cargar.
    /// </summary>
    [RelayCommand]
    private void AgregarBillete(BilleteTec? billete)
    {
        if (billete is null)
        {
            return;
        }

        if ((DateTime.UtcNow - _cargadoEnUtc).TotalMilliseconds < MsIgnorarToggleTrasCarga)
        {
            return;
        }

        billete.Agregado = !billete.Agregado;
        NotificarDerivadas();
    }

    /// <summary>
    /// Carrito del header: abre el carrito Tec (pantalla 14).
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

    /// <summary>
    /// Retrocede a la pantalla anterior (Seleccionar Sorteo).
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

    private void NotificarDerivadas()
    {
        OnPropertyChanged(nameof(BadgeTexto));
        OnPropertyChanged(nameof(HayEnCarrito));
        OnPropertyChanged(nameof(SinBilletes));
    }
}
