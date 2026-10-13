using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Ventas" (mockup 5): listado de ventas
/// REALES del billetero de la sesión (GET api/mobile/ventas) con filtro
/// por tipo en overlay. Estados: cargando (overlay), vacío, error con
/// reintento por re-entrada (patrón Depósitos).
/// </summary>
public partial class TicketsVentaViewModel : BaseViewModel
{
    private readonly TicketsVentaService _servicio;

    /// <summary>Movimientos visibles (con filtro aplicado).</summary>
    public ObservableCollection<MovimientoVenta> Movimientos { get; } = [];

    /// <summary>Historial completo cargado del backend (para re-filtrar sin re-consultar).</summary>
    private List<MovimientoVenta> _historial = [];

    /// <summary>Opciones del filtro (null = Todos).</summary>
    public IReadOnlyList<TipoVenta?> Filtros { get; } =
        [null, TipoVenta.Lotenal, TipoVenta.SorteosTec, TipoVenta.TiempoAire];

    /// <summary>Filtro activo (null = Todos).</summary>
    [ObservableProperty]
    private TipoVenta? filtroTipo;

    /// <summary>Overlay del filtro abierto/cerrado.</summary>
    [ObservableProperty]
    private bool filtroVisible;

    /// <summary>True mientras corre el GET del historial (overlay).</summary>
    [ObservableProperty]
    private bool cargando;

    /// <summary>Mensaje de error de la carga (reintentar re-entrando).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True cuando hay mensaje de error (muestra el aviso rojo).</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>True cuando la carga terminó sin ventas (estado vacío).</summary>
    public bool EstaVacio => !Cargando && !HayError && Movimientos.Count == 0;

    /// <summary>Mensaje del estado vacío.</summary>
    [ObservableProperty]
    private string mensajeVacio = "No hay ventas registradas";

    /// <summary>
    /// Timestamp del momento en que se abrió el overlay (protección
    /// anti tap-fantasma durante la transición).
    /// </summary>
    private long _abiertoEnUtc;

    /// <summary>Ms que se ignora el toque del scrim tras abrir.</summary>
    private const int MsIgnorarCierreTrasAbrir = 600;

    /// <summary>Texto del botón filtro: "Todos", "LOTENAL", etc.</summary>
    public string FiltroText => FiltroTipo switch
    {
        TipoVenta.Lotenal => "LOTENAL",
        TipoVenta.SorteosTec => "Sorteos Tec",
        TipoVenta.TiempoAire => "Tiempo Aire",
        _ => "Todos",
    };

    /// <summary>True si hay un filtro distinto de Todos aplicado.</summary>
    public bool FiltroActivo => FiltroTipo is not null;

    /// <summary>Marcas del filtro (check verde por opción activa).</summary>
    public bool EsTodos => FiltroTipo is null;

    public bool EsLotenal => FiltroTipo == TipoVenta.Lotenal;

    public bool EsTec => FiltroTipo == TipoVenta.SorteosTec;

    public bool EsTa => FiltroTipo == TipoVenta.TiempoAire;

    public TicketsVentaViewModel(TicketsVentaService servicio)
    {
        _servicio = servicio;
        Title = "Ventas";
    }

    /// <summary>Carga el historial real al entrar (y al volver del detalle).</summary>
    public async Task AlAparecerAsync()
    {
        if (Cargando)
        {
            return;
        }

        Cargando = true;
        MensajeError = string.Empty;
        try
        {
            try
            {
                List<VentaHistorialApi> items = await _servicio.CargarVentasAsync();
                _historial = items.Select(MovimientoVenta.DesdeHistorial).ToList();
                AplicarFiltro();
            }
            catch (TaskCanceledException)
            {
                MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            }
            catch (OperationCanceledException)
            {
                MensajeError = "Consulta cancelada";
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            }
            catch (ApiException ex)
            {
                MensajeError = $"No se pudieron cargar las ventas: {ex.Message}";
            }
        }
        finally
        {
            Cargando = false;
            // Derivadas notificadas al FINAL (con Cargando ya en false) —
            // notificarlas dentro del try dejaba EstaVacio siempre en
            // false (patrón Depósitos/Recibos).
            OnPropertyChanged(nameof(HayError));
            OnPropertyChanged(nameof(EstaVacio));
        }
    }

    /// <summary>Aplica el filtro activo sobre el historial cargado.</summary>
    private void AplicarFiltro()
    {
        Movimientos.Clear();
        foreach (MovimientoVenta m in _historial
                     .Where(m => FiltroTipo is null || m.Tipo == FiltroTipo))
        {
            Movimientos.Add(m);
        }
        OnPropertyChanged(nameof(FiltroText));
    }

    /// <summary>Abre el overlay del filtro.</summary>
    [RelayCommand]
    private void AbrirFiltro()
    {
        FiltroVisible = true;
        _abiertoEnUtc = System.Diagnostics.Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Cierra el overlay tocando el scrim; ignora el toque que atraviesa
    /// la transición de apertura (~600 ms).
    /// </summary>
    [RelayCommand]
    private void CerrarFiltro()
    {
        long ahora = System.Diagnostics.Stopwatch.GetTimestamp();
        long transcurridoMs = (ahora - _abiertoEnUtc) * 1000
                              / System.Diagnostics.Stopwatch.Frequency;
        if (transcurridoMs < MsIgnorarCierreTrasAbrir)
        {
            return;
        }
        FiltroVisible = false;
    }

    /// <summary>Aplica una opción del filtro y cierra el overlay.</summary>
    [RelayCommand]
    private void SeleccionarFiltro(string? tipo)
    {
        FiltroTipo = string.IsNullOrEmpty(tipo)
            ? null
            : Enum.TryParse<TipoVenta>(tipo, out TipoVenta t) ? t : null;
        FiltroVisible = false;
        AplicarFiltro();
        OnPropertyChanged(nameof(FiltroText));
        OnPropertyChanged(nameof(FiltroActivo));
        OnPropertyChanged(nameof(EsTodos));
        OnPropertyChanged(nameof(EsLotenal));
        OnPropertyChanged(nameof(EsTec));
        OnPropertyChanged(nameof(EsTa));
    }

    /// <summary>Abre el detalle de la venta seleccionada (navega por id_venta).</summary>
    [RelayCommand]
    private async Task AbrirDetalleAsync(MovimientoVenta movimiento)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await Shell.Current.GoToAsync(
                $"{nameof(Views.DetalleVentaPage)}?id={movimiento.IdVenta}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Regresa a Gestión.</summary>
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
