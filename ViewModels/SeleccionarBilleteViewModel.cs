using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

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
/// El estado Agregado se refleja desde el carrito compartido (CarritoTecService).
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
                _ = IntentarCargarAsync();
            }
        }
    }

    private readonly MobileVentasService _ventas;
    private readonly ISessionService _session;

    public SeleccionarBilleteViewModel(MobileVentasService ventas, ISessionService session)
    {
        Title = "Seleccionar Billete";
        _ventas = ventas;
        _session = session;
    }

    // ============ Derivados (notificar con NotificarDerivadas) ============

    /// <summary>Badge del carrito del header: boletos agregados (todos los sorteos).</summary>
    public string BadgeTexto => SorteosTecViewModel.Carrito.Billetes.Count.ToString();

    /// <summary>True con al menos un billete agregado (muestra el badge).</summary>
    public bool HayEnCarrito => SorteosTecViewModel.Carrito.HayBilletes;

    /// <summary>True sin billetes en el sorteo (mensaje "Sin billetes disponibles").</summary>
    public bool SinBilletes => Billetes.Count == 0;

    /// <summary>
    /// Carga la lista de boletos REALES del sorteo (api/mobile/ventas/
    /// boletos-tec). Antes usaba SorteosTecData, cuyo catálogo fijo traía
    /// un billete inventado por sorteo con un id que no existe en la BD:
    /// venderlo nunca habría registrado nada.
    /// </summary>
    private async Task IntentarCargarAsync()
    {
        if (_cargado || _sorteoId is null || IsBusy)
        {
            return;
        }

        _cargado = true;
        IsBusy = true;
        try
        {
            NombrePantalla = "Cargando...";
            IReadOnlyList<BilleteTec> lista = await _ventas.BilletesTecDisponiblesAsync(_sorteoId.Value);
            Billetes = new ObservableCollection<BilleteTec>(lista);
            // Cache en memoria para que el carrito (14) pueda resolver el
            // nombre y el precio del sorteo de cada boleto.
            SorteosTecCatalogo.CargarBilletes(lista);

            // Refleja la pleca verde en los boletos que ya estaban en el
            // carrito persistido (p. ej. se volvió de otra pantalla).
            foreach (BilleteTec b in Billetes)
            {
                b.Agregado = SorteosTecViewModel.Carrito.IdsBilletes().Contains(b.IdBillete);
            }

            // Sin nombre de sorteo en esta pantalla: el titulo real lo trae
            // la tarjeta de la pantalla 12. Se muestra el id solo como
            // referencia, nunca un nombre inventado.
            NombrePantalla = $"Sorteo {_sorteoId.Value}";
            Title = NombrePantalla;
        }
        catch (Exception)
        {
            // Red caída o sin material asignado: lista vacía, sin inventar.
            Billetes = new ObservableCollection<BilleteTec>();
            NombrePantalla = "Sin billetes disponibles";
        }
        finally
        {
            _cargadoEnUtc = DateTime.UtcNow;
            IsBusy = false;
            NotificarDerivadas();
        }
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
    ///
    /// Persiste en SecureStorage: si la app se cierra, el boleto sigue en
    /// el carrito al volver.
    /// </summary>
    [RelayCommand]
    private async Task AgregarBillete(BilleteTec? billete)
    {
        if (billete is null)
        {
            return;
        }

        if ((DateTime.UtcNow - _cargadoEnUtc).TotalMilliseconds < MsIgnorarToggleTrasCarga)
        {
            return;
        }

        SorteoTec? sorteo = SorteosTecCatalogo.ObtenerSorteo(billete.IdSorteo);
        await SorteosTecViewModel.Carrito.AlternarAsync(
            _session.CurrentSession?.Billetero?.IdBilletero ?? 0,
            new CarritoTecItem
            {
                IdBillete = billete.IdBillete,
                IdSorteo = sorteo?.IdSorteo ?? billete.IdSorteo,
                Numero = billete.Numero,
                NombreSorteo = sorteo?.NombreSorteo ?? string.Empty,
                Precio = sorteo?.Precio ?? 0m,
                ColorHex = sorteo?.ColorHex ?? string.Empty,
            });

        billete.Agregado = SorteosTecViewModel.Carrito.IdsBilletes().Contains(billete.IdBillete);
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
