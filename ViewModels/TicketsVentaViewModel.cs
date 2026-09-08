using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Ventas" (mockup 5): listado de ventas de
/// Vender (LOTENAL, Sorteos Tec, Tiempo Aire) con filtro por tipo en
/// overlay. Fase SOLO INTERFAZ — movimientos temporales del
/// TicketsVentaService (ver docs/NOTAS_TICKETS_VENTA.md).
/// </summary>
public partial class TicketsVentaViewModel : BaseViewModel
{
    private readonly TicketsVentaService _servicio;

    /// <summary>Movimientos visibles (con filtro aplicado).</summary>
    public ObservableCollection<MovimientoVenta> Movimientos { get; } = [];

    /// <summary>Opciones del filtro (null = Todos).</summary>
    public IReadOnlyList<TipoVenta?> Filtros { get; } =
        [null, TipoVenta.Lotenal, TipoVenta.SorteosTec, TipoVenta.TiempoAire];

    /// <summary>Filtro activo (null = Todos).</summary>
    [ObservableProperty]
    private TipoVenta? filtroTipo;

    /// <summary>Overlay del filtro abierto/cerrado.</summary>
    [ObservableProperty]
    private bool filtroVisible;

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

    /// <summary>Carga la lista al aparecer (respeta el filtro activo).</summary>
    public void AlAparecer()
    {
        Recargar();
    }

    /// <summary>Aplica el filtro y refresca la lista.</summary>
    private void Recargar()
    {
        Movimientos.Clear();
        foreach (MovimientoVenta m in _servicio.Movimientos()
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
        Recargar();
        OnPropertyChanged(nameof(FiltroText));
        OnPropertyChanged(nameof(FiltroActivo));
        OnPropertyChanged(nameof(EsTodos));
        OnPropertyChanged(nameof(EsLotenal));
        OnPropertyChanged(nameof(EsTec));
        OnPropertyChanged(nameof(EsTa));
    }

    /// <summary>Abre el detalle del movimiento seleccionado.</summary>
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
                $"{nameof(Views.DetalleVentaPage)}?folio={movimiento.Folio}");
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
