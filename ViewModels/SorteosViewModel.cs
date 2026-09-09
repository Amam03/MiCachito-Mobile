using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla Sorteos (mockups 4, 4.1, 4.2): selector de
/// sorteos celebrados FIJADO en la parte superior (se puede cambiar de
/// sorteo en cualquier momento sin salir de la vista), resumen de 3
/// columnas, secciones colapsables Entregas y Devoluciones / Pagos
/// Realizados. Fase SOLO INTERFAZ (ver docs/NOTAS_SORTEOS.md).
/// </summary>
public partial class SorteosViewModel : BaseViewModel
{
    private readonly SorteosService _servicio;

    /// <summary>Opciones del dropdown (celebrados, fecha descendente).</summary>
    public ObservableCollection<SorteoActivoLotenal> Sorteos { get; } = [];

    /// <summary>Sorteo seleccionado (null = estado inicial sin información).</summary>
    [ObservableProperty]
    private SorteoActivoLotenal? sorteoSeleccionado;

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

    /// <summary>Texto del selector: nombre del sorteo o "Seleccionar Sorteo".</summary>
    public string SelectorText =>
        SorteoSeleccionado is null
            ? "Seleccionar Sorteo"
            : $"{SorteoSeleccionado.NombreCorto} - {SorteoSeleccionado.FechaLarga}";

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

    /// <summary>Carga el listado al aparecer la página.</summary>
    public void AlAparecer()
    {
        Sorteos.Clear();
        foreach (SorteoActivoLotenal s in _servicio.SorteosCelebrados())
        {
            Sorteos.Add(s);
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
    /// su información y cierra el dropdown.
    /// </summary>
    [RelayCommand]
    private void SeleccionarSorteo(SorteoActivoLotenal sorteo)
    {
        SorteoSeleccionado = sorteo;
        Info = _servicio.ObtenerInfo(sorteo);
        DropdownVisible = false;

        // Reiniciar secciones colapsadas al cambiar de sorteo
        EntregasExpandido = false;
        PagosExpandido = false;

        if (Info is not null)
        {
            VentasText = $"${Info.Ventas:0.00}";
            PagosRealizadosText = $"${Info.PagosRealizados:0.00}";
            SaldoAPagarText = $"${Info.SaldoAPagar:0.00}";
            TotalEntregasText = $"${Info.TotalEntregas:0.00}";
            DevolucionText = $"${Info.TotalDevolucion:0.00}";
            VentasEntregasText = $"${Info.VentasEntregas:0.00}";
            TotalPagadoText = $"${Info.TotalPagado:0.00}";
        }

        OnPropertyChanged(nameof(SelectorText));
        OnPropertyChanged(nameof(HaySeleccion));
        OnPropertyChanged(nameof(SinSeleccion));
    }

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
