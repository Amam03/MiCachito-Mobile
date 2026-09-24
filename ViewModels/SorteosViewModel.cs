using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla Sorteos (mockups 4, 4.1, 4.2): selector de
/// sorteos registrados FIJADO en la parte superior (se puede cambiar de
/// sorteo en cualquier momento sin salir de la vista), resumen de 3
/// columnas, secciones colapsables Entregas y Devoluciones / Pagos
/// Realizados. FASE 2 INTEGRACIÓN (2026-09-23): lista y detalle reales
/// de GET api/mobile/sorteos con manejo de carga, errores y vacío
/// (patrón Reportes; sin inventar datos).
/// </summary>
public partial class SorteosViewModel : BaseViewModel
{
    private readonly SorteosService _servicio;

    /// <summary>Opciones del dropdown (registrados, fecha descendente).</summary>
    public ObservableCollection<SorteoCelebradoInfo> Sorteos { get; } = [];

    /// <summary>Sorteo seleccionado (null = estado inicial sin información).</summary>
    [ObservableProperty]
    private SorteoCelebradoInfo? sorteoSeleccionado;

    /// <summary>Info agregada del sorteo seleccionado (null = sin selección).</summary>
    [ObservableProperty]
    private SorteoCelebradoInfo? info;

    /// <summary>Dropdown abierto/cerrado.</summary>
    [ObservableProperty]
    private bool dropdownVisible;

    /// <summary>Entregas y Devoluciones expandido.</summary>
    [ObservableProperty]
    private bool entregasExpandido;

    /// <summary>Pagos Realizados expandido.</summary>
    [ObservableProperty]
    private bool pagosExpandido;

    /// <summary>True mientras carga la lista al aparecer (overlay).</summary>
    [ObservableProperty]
    private bool cargandoLista;

    /// <summary>True mientras carga el detalle del sorteo (overlay).</summary>
    [ObservableProperty]
    private bool cargandoDetalle;

    /// <summary>Mensaje de error de la carga de lista o detalle.</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True cuando hay error visible.</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>True cuando la lista cargó y NO hay sorteos (estado vacío).</summary>
    [ObservableProperty]
    private bool sinSorteos;

    // ── Textos derivados del resumen (mockup 4.1) ────────────────────
    [ObservableProperty]
    private string ventasText = "$0.00";

    [ObservableProperty]
    private string pagosRealizadosText = "$0.00";

    [ObservableProperty]
    private string saldoAPagarText = "$0.00";

    [ObservableProperty]
    private string totalEntregasText = "$0.00";

    [ObservableProperty]
    private string devolucionText = "$0.00";

    [ObservableProperty]
    private string ventasEntregasText = "$0.00";

    [ObservableProperty]
    private string totalPagadoText = "$0.00";

    [ObservableProperty]
    private string pagosAlMomentoText = "$0.00";

    /// <summary>Texto del selector: label del sorteo o "Seleccionar Sorteo".</summary>
    public string SelectorText =>
        SorteoSeleccionado is null
            ? "Seleccionar Sorteo"
            : $"{SorteoSeleccionado.Sorteo} - {SorteoSeleccionado.FechaLarga}";

    /// <summary>Flecha del selector (siempre ▼: abre el dropdown).</summary>
    public string SelectorFlecha => "\u25BC";

    /// <summary>True cuando hay sorteo seleccionado (muestra el contenido).</summary>
    public bool HaySeleccion => SorteoSeleccionado is not null;

    /// <summary>Inverso: mensaje de estado inicial visible.</summary>
    public bool SinSeleccion => !HaySeleccion;

    /// <summary>Flecha de Entregas y Devoluciones (▼ colapsado / ▲ expandido).</summary>
    public string FlechaEntregas => EntregasExpandido ? "\u25B2" : "\u25BC";

    /// <summary>Flecha de Pagos Realizados (▼ colapsado / ▲ expandido).</summary>
    public string FlechaPagos => PagosExpandido ? "\u25B2" : "\u25BC";

    public SorteosViewModel(SorteosService servicio)
    {
        _servicio = servicio;
        Title = "Sorteos";
    }

    /// <summary>
    /// Carga la lista real al aparecer la página (y en re-entradas tras
    /// error: el usuario puede reintentar volviendo a entrar).
    /// </summary>
    public async Task AlAparecerAsync()
    {
        if (CargandoLista)
        {
            return;
        }

        CargandoLista = true;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(HayError));
        try
        {
            Sorteos.Clear();
            IReadOnlyList<SorteoCelebradoInfo> lista =
                await _servicio.SorteosRegistradosAsync();

            foreach (SorteoCelebradoInfo s in lista)
            {
                Sorteos.Add(s);
            }

            SinSorteos = Sorteos.Count == 0;
        }
        catch (TaskCanceledException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (OperationCanceledException)
        {
            MensajeError = "Consulta cancelada";
            OnPropertyChanged(nameof(HayError));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (ApiException ex)
        {
            MensajeError = ex.Message;
            OnPropertyChanged(nameof(HayError));
        }
        finally
        {
            CargandoLista = false;
        }
    }

    /// <summary>Abre/cierra el dropdown de selección.</summary>
    [RelayCommand]
    private void ToggleDropdown()
    {
        DropdownVisible = !DropdownVisible;
    }

    /// <summary>
    /// Selecciona un sorteo del dropdown: fija el selector arriba, carga
    /// su detalle REAL y cierra el dropdown. Al cambiar de sorteo se
    /// limpia TODO (info, textos, secciones) antes de cargar.
    /// </summary>
    [RelayCommand]
    private async Task SeleccionarSorteoAsync(SorteoCelebradoInfo sorteo)
    {
        if (CargandoDetalle || sorteo is null)
        {
            return;
        }

        SorteoSeleccionado = sorteo;
        DropdownVisible = false;

        // Limpiar TODO al cambiar de sorteo (datos viejos fuera).
        Info = null;
        EntregasExpandido = false;
        PagosExpandido = false;
        OnPropertyChanged(nameof(FlechaEntregas));
        OnPropertyChanged(nameof(FlechaPagos));
        LimpiarTextos();
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(HayError));
        OnPropertyChanged(nameof(SelectorText));
        OnPropertyChanged(nameof(HaySeleccion));
        OnPropertyChanged(nameof(SinSeleccion));

        CargandoDetalle = true;
        try
        {
            SorteoCelebradoInfo? detalle = await _servicio.ObtenerDetalleAsync(
                sorteo.IdSorteo, sorteo.NumeroSorteo);
            if (detalle is null)
            {
                MensajeError = "El detalle del sorteo no está disponible";
                OnPropertyChanged(nameof(HayError));
                return;
            }

            Info = detalle;
            AplicarTextos(detalle);
        }
        catch (TaskCanceledException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (OperationCanceledException)
        {
            MensajeError = "Consulta cancelada";
            OnPropertyChanged(nameof(HayError));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (ApiException ex)
        {
            // 404: el sorteo no es del billetero o ya no tiene registros.
            MensajeError = ex.Message;
            OnPropertyChanged(nameof(HayError));
        }
        finally
        {
            CargandoDetalle = false;
        }
    }

    /// <summary>Puebla los textos del resumen/secciones con el detalle.</summary>
    private void AplicarTextos(SorteoCelebradoInfo info)
    {
        VentasText = FormatoMoneda(info.Ventas);
        PagosRealizadosText = FormatoMoneda(info.PagosRealizados);
        SaldoAPagarText = FormatoMoneda(info.SaldoAPagar);
        TotalEntregasText = FormatoMoneda(info.TotalEntregas);
        DevolucionText = FormatoMoneda(info.TotalDevolucion);
        VentasEntregasText = FormatoMoneda(info.VentasEntregas);
        TotalPagadoText = FormatoMoneda(info.TotalPagado);
        PagosAlMomentoText = FormatoMoneda(info.PagosAlMomento);
    }

    /// <summary>Resetea los textos a $0.00 (cambio de sorteo / sin datos).</summary>
    private void LimpiarTextos()
    {
        VentasText = "$0.00";
        PagosRealizadosText = "$0.00";
        SaldoAPagarText = "$0.00";
        TotalEntregasText = "$0.00";
        DevolucionText = "$0.00";
        VentasEntregasText = "$0.00";
        TotalPagadoText = "$0.00";
        PagosAlMomentoText = "$0.00";
    }

    /// <summary>Formato de moneda de la pantalla ("$1,234.50").</summary>
    private static string FormatoMoneda(decimal v) =>
        $"${v:N2}";

    /// <summary>Expande/contrae Entregas y Devoluciones.</summary>
    [RelayCommand]
    private void ToggleEntregas()
    {
        EntregasExpandido = !EntregasExpandido;
        OnPropertyChanged(nameof(FlechaEntregas));
    }

    /// <summary>Expande/contrae Pagos Realizados.</summary>
    [RelayCommand]
    private void TogglePagos()
    {
        PagosExpandido = !PagosExpandido;
        OnPropertyChanged(nameof(FlechaPagos));
    }

    /// <summary>Cierra el dropdown tocando el scrim.</summary>
    [RelayCommand]
    private void CerrarDropdown()
    {
        DropdownVisible = false;
    }

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
