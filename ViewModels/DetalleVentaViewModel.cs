using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel del "Detalle de Venta" (mockup 5.1): datos del movimiento
/// según tipo (LOTENAL/Tec: producto, boleto, valor; Tiempo Aire:
/// compañía, teléfono, monto/paquete) + acciones Imprimir y Compartir.
/// Recibe el folio vía [QueryProperty] y carga en AlAparecer (patrón
/// del proyecto). Fase SOLO INTERFAZ.
/// </summary>
[QueryProperty(nameof(FolioQuery), "folio")]
public partial class DetalleVentaViewModel : BaseViewModel
{
    private readonly TicketsVentaService _tickets;
    private readonly TicketPdfService _pdf;
    private readonly IImpresoraService _impresora;

    /// <summary>Movimiento mostrado (null mientras carga).</summary>
    [ObservableProperty]
    private MovimientoVenta? movimiento;

    /// <summary>Folio recibido por navegación Shell (string).</summary>
    public string? FolioQuery { get; set; }

    public DetalleVentaViewModel(
        TicketsVentaService tickets,
        TicketPdfService pdf,
        IImpresoraService impresora)
    {
        _tickets = tickets;
        _pdf = pdf;
        _impresora = impresora;
        Title = "Detalle de Venta";
    }

    /// <summary>Carga el movimiento al aparecer la página.</summary>
    public void AlAparecer()
    {
        int folio = int.TryParse(FolioQuery, out int f) ? f : 0;
        Movimiento = _tickets.ObtenerPorFolio(folio);
        OnPropertyChanged(nameof(Cliente));
        OnPropertyChanged(nameof(ImporteText));
        OnPropertyChanged(nameof(ProductoOCompania));
        OnPropertyChanged(nameof(EtiquetaProducto));
        OnPropertyChanged(nameof(DatoPrincipal));
        OnPropertyChanged(nameof(EtiquetaDatoPrincipal));
        OnPropertyChanged(nameof(ValorOMonto));
        OnPropertyChanged(nameof(EtiquetaValor));
        OnPropertyChanged(nameof(FechaText));
        OnPropertyChanged(nameof(FolioText));
    }

    // ── Derivados para el binding del detalle ─────────────────────────

    public string Cliente => Movimiento?.Cliente ?? string.Empty;

    public string ImporteText => Movimiento?.ImporteText ?? "$0.00";

    /// <summary>Producto (Lotenal/Tec) o compañía (TA).</summary>
    public string ProductoOCompania =>
        Movimiento?.Tipo is TipoVenta.Lotenal or TipoVenta.SorteosTec
            ? Movimiento.Sorteo ?? string.Empty
            : Movimiento?.Compania ?? string.Empty;

    /// <summary>Etiqueta del producto según tipo.</summary>
    public string EtiquetaProducto =>
        Movimiento?.Tipo is TipoVenta.Lotenal or TipoVenta.SorteosTec
            ? "Producto / Sorteo"
            : "Compañía";

    /// <summary>Boleto o teléfono según tipo.</summary>
    public string DatoPrincipal =>
        Movimiento?.Tipo is TipoVenta.Lotenal or TipoVenta.SorteosTec
            ? Movimiento.Boleto ?? string.Empty
            : Movimiento?.Telefono ?? string.Empty;

    /// <summary>Etiqueta del dato principal según tipo.</summary>
    public string EtiquetaDatoPrincipal =>
        Movimiento?.Tipo is TipoVenta.Lotenal or TipoVenta.SorteosTec
            ? "Boleto"
            : "Teléfono";

    /// <summary>Valor del boleto (Lotenal/Tec) o monto/paquete (TA).</summary>
    public string ValorOMonto =>
        Movimiento?.Tipo is TipoVenta.Lotenal or TipoVenta.SorteosTec
            ? Movimiento.Valor is { } v ? $"${v:0.00}" : string.Empty
            : Movimiento?.MontoPaquete ?? string.Empty;

    /// <summary>Etiqueta del valor según tipo.</summary>
    public string EtiquetaValor =>
        Movimiento?.Tipo is TipoVenta.Lotenal or TipoVenta.SorteosTec
            ? "Valor"
            : "Monto / Paquete";

    public string FechaText => Movimiento?.FechaText ?? string.Empty;

    public string FolioText => Movimiento is null ? string.Empty : $"Folio: {Movimiento.Folio}";

    // ── Acciones (mockup 5.1: botones circulares inferiores) ──────────

    /// <summary>
    /// Imprimir: prepara el flujo Bluetooth; sin impresora vinculada
    /// muestra el aviso (caso emulador). Lógica real documentada en
    /// docs/NOTAS_TICKETS_VENTA.md.
    /// </summary>
    [RelayCommand]
    private async Task ImprimirAsync()
    {
        if (IsBusy || Movimiento is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            string rutaPdf = await _pdf.GenerarAsync(Movimiento);
            string resultado = await _impresora.ImprimirAsync(rutaPdf, "Ticket de Venta");
            if (Application.Current?.MainPage is not null)
            {
                await Application.Current.MainPage.DisplayAlert("Imprimir", resultado, "Aceptar");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Compartir: genera el PDF y abre el diálogo nativo.</summary>
    [RelayCommand]
    private async Task CompartirAsync()
    {
        if (IsBusy || Movimiento is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            string rutaPdf = await _pdf.GenerarAsync(Movimiento);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Ticket de Venta",
                File = new ShareFile(rutaPdf),
            });
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Regresa a la lista de ventas.</summary>
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
