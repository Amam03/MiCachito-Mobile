using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

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
    /// <summary>
    /// Sorteos TEC del billetero de la sesión (material asignado).
    /// Empty mientras carga: la lista se puebla desde la API.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SorteoTec> _sorteos = new();

    /// <summary>
    /// IDs de los boletos agregados al carrito. Antes vivía en
    /// SorteosTecData (catálogo fijo); ahora el carrito es estado de la
    /// pantalla, porque los boletos vienen de la API y sus ids son reales.
    /// </summary>
    private readonly HashSet<int> _idsEnCarrito = new();

    /// <summary>
    /// Carrito COMPARTIDO y PERSISTIDO de las pantallas TEC (12, 13, 14 y 15).
    ///
    /// Antes el estado vivía en SorteosTecData, un catálogo estático con
    /// boletos inventados. Ahora lo lleva <see cref="CarritoTecService"/>,
    /// que es Singleton (las pantallas son instancias distintas del mismo
    /// flujo) y persiste en SecureStorage: si la app se cierra a mitad de la
    /// venta, al volver siguen los boletos que el billetero había agregado.
    ///
    /// No es un inventario paralelo: solo guarda QUÉ eligió el billetero.
    /// El estado de venta sigue siendo el del backend, y al confirmar se
    /// revalidan los precios y la pertenencia (409/403 si ya no aplica).
    /// </summary>
    private static CarritoTecService? _carrito;

    /// <summary>
    /// Se asigna una vez por la pantalla 12 (la primera del flujo). Las demás
    /// pantallas lo reciben por constructor desde el contenedor DI.
    /// </summary>
    public static CarritoTecService Carrito => _carrito
        ?? throw new InvalidOperationException(
            "El carrito TEC no está inicializado: la pantalla 12 debe crearlo primero.");

    /// <summary>
    /// Registra el carrito compartido. Lo hace SorteosTecPage al construirse;
    /// así el resto del flujo usa SIEMPRE la misma instancia, en vez de
    /// depender del orden en que DI construya las pantallas.
    /// </summary>
    public static void RegistrarCarrito(CarritoTecService carrito) => _carrito = carrito;

    private readonly MobileVentasService _ventas;
    private readonly ISessionService _session;
    private readonly CarritoTecService _carritoTec;

    /// <summary>
    /// Inyectado por constructor (mismo patrón que SorteosViewModel con
    /// SorteosService): el contenedor de MauiProgram lo resuelve.
    /// </summary>
    public SorteosTecViewModel(
        MobileVentasService ventas,
        ISessionService session,
        CarritoTecService carrito)
    {
        Title = "Seleccionar Sorteo";
        _ventas = ventas;
        _session = session;
        _carritoTec = carrito;

        // Una sola instancia compartida por las cuatro pantallas del flujo.
        RegistrarCarrito(carrito);
    }

    /// <summary>
    /// Carga los sorteos TEC del billetero y RESTAURA el carrito persistido.
    /// Se invoca desde OnAppearing de la pantalla 12 (y tras volver del
    /// carrito).
    /// </summary>
    public async Task CargarAsync()
    {
        if (IsBusy)
        {
            NotificarDerivadas();
            return;
        }

        IsBusy = true;
        try
        {
            IReadOnlyList<SorteoTec> lista = await _ventas.SorteosTecDisponiblesAsync();
            // Cache en memoria: el carrito (14) necesita nombre y precio del
            // sorteo de cada boleto, y el boleto por si solo no los trae.
            SorteosTecCatalogo.CargarSorteos(lista);
            Sorteos = new ObservableCollection<SorteoTec>(lista);

            // Restaura el carrito persistido. Si el boleto ya no esta
            // disponible (otro canal lo vendio, o el material fue devuelto
            // mientras la app estaba cerrada), se descarta SOLO ese: el
            // resto del carrito sobrevive.
            SessionInfo? sesion = await _session.LoadAsync();
            if (sesion?.Billetero is not null)
            {
                await _carritoTec.CargarAsync(sesion.Billetero.IdBilletero);
            }
        }
        catch (Exception)
        {
            // Red caída o backend sin material: la lista queda vacía. No se
            // rellena con el catálogo fijo, porque sus ids están desfasados.
            SorteosTecCatalogo.LimpiarTodo();
            Sorteos = new ObservableCollection<SorteoTec>();
        }
        finally
        {
            IsBusy = false;
            NotificarDerivadas();
        }
    }

    /// <summary>Badge del carrito flotante: boletos agregados.</summary>
    public string BadgeTexto => _carritoTec.HayBilletes ? _carritoTec.Billetes.Count.ToString() : "0";

    /// <summary>True si hay boletos en el carrito (muestra el badge).</summary>
    public bool HayEnCarrito => _carritoTec.HayBilletes;

    /// <summary>
    /// Al volver de la 13/14 (OnAppearing): refresca el badge.
    /// </summary>
    public void AlAparecer()
    {
        NotificarDerivadas();
    }

    /// <summary>
    /// IDs de los boletos actualmente en el carrito. Lo consume el carrito
    /// (14) y la pantalla de datos del cliente (15).
    /// </summary>
    public IReadOnlyList<int> BilletesEnCarrito() => _carritoTec.IdsBilletes();

    /// <summary>
    /// Marca/desmarca un boleto en el carrito (pleca verde de la pantalla 13)
    /// y persiste el cambio.
    /// </summary>
    public Task AlternarBilleteAsync(BilleteTec billete, SorteoTec? sorteo)
    {
        return _carritoTec.AlternarAsync(
            IdBilleteroActual(),
            new CarritoTecItem
            {
                IdBillete = billete.IdBillete,
                IdSorteo = billete.IdSorteo,
                Numero = billete.Numero,
                NombreSorteo = sorteo?.NombreSorteo ?? string.Empty,
                Precio = sorteo?.Precio ?? 0m,
                ColorHex = sorteo?.ColorHex ?? string.Empty,
            });
    }

    /// <summary>True si el boleto está en el carrito (pleca de la fila 13).</summary>
    public bool EstaEnCarrito(int idBillete) => _carritoTec.IdsBilletes().Contains(idBillete);

    /// <summary>
    /// Id del billetero de la sesión, para scope del carrito persistido.
    /// </summary>
    private int IdBilleteroActual() => _session.CurrentSession?.Billetero?.IdBilletero ?? 0;

    /// <summary>
    /// Retrocede al Home (flecha del header): la página se abre con
    /// GoToAsync relativo desde el Home, pop de un nivel.
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
