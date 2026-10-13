using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel del "Detalle de Venta" (mockup 5.1): datos de la venta REAL
/// (GET api/mobile/ventas/{id}) según tipo (LOTENAL/Tec: producto,
/// boleto, valor; Tiempo Aire: compañía, teléfono, monto/paquete) +
/// acciones Imprimir y Compartir. Recibe el id_venta vía [QueryProperty]
/// y carga en AlAparecerAsync (patrón del proyecto). 404 si la venta es
/// ajena: se muestra el aviso y no se navega a datos de otro billetero.
/// </summary>
[QueryProperty(nameof(IdQuery), "id")]
public partial class DetalleVentaViewModel : BaseViewModel
{
    private readonly TicketsVentaService _tickets;
    private readonly TicketPdfService _pdf;
    private readonly IImpresoraService _impresora;

    /// <summary>Movimiento mostrado (null mientras carga).</summary>
    [ObservableProperty]
    private MovimientoVenta? movimiento;

    /// <summary>Id de venta recibido por navegación Shell (string).</summary>
    public string? IdQuery { get; set; }

    [ObservableProperty]
    private bool cargando;

    [ObservableProperty]
    private string mensajeError = string.Empty;

    public bool HayError => !string.IsNullOrEmpty(MensajeError);

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

    /// <summary>Carga la venta real al aparecer la página.</summary>
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
                if (!int.TryParse(IdQuery, out int id) || id <= 0)
                {
                    MensajeError = "Venta no válida";
                    return;
                }

                VentaDetalleApi detalle = await _tickets.CargarDetalleAsync(id);
                Movimiento = MovimientoVenta.DesdeDetalle(detalle);
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
                MensajeError = $"No se pudo cargar la venta: {ex.Message}";
            }
        }
        finally
        {
            Cargando = false;
            OnPropertyChanged(nameof(HayError));
            NotificarDerivadas();
        }
    }

    /// <summary>Notifica las propiedades derivadas del movimiento.</summary>
    private void NotificarDerivadas()
    {
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

    /// <summary>Boleto(s) o teléfono según tipo.</summary>
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
            if (Application.Current is not null && Application.Current.Windows.Count > 0)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync("Imprimir", resultado, "Aceptar");
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
