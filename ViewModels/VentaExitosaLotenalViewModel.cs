using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Venta Exitosa LOTENAL" (F3): se llega tras
/// confirmar la venta en el carrito (pantalla 11). Muestra el FOLIO y
/// el TOTAL REALES devueltos por el backend (POST
/// api/mobile/ventas/crear) más el resumen de boletos vendidos.
///
/// F6 — IMPRESIÓN: botón "Imprimir comprobante" con los datos reales
/// de la venta (folio/total/boletos + producto/dotación y desglose de
/// SesionVentaLotenal, viva hasta Continuar). La impresión NUNCA
/// repite la venta: solo envía texto ESC/POS a la impresora; error →
/// aviso con "Reintentar" y "Seleccionar impresora".
/// </summary>
[QueryProperty(nameof(Folio), "folio")]
[QueryProperty(nameof(TotalTexto), "total")]
[QueryProperty(nameof(BoletosTexto), "boletos")]
public partial class VentaExitosaLotenalViewModel : BaseViewModel
{
    private readonly IImpresoraService _impresora;

    /// <summary>Folio real de la venta ("V-20260925-67242").</summary>
    [ObservableProperty]
    private string folio = string.Empty;

    /// <summary>Total real formateado ("$140.00").</summary>
    [ObservableProperty]
    private string totalTexto = string.Empty;

    /// <summary>Resumen de boletos vendidos ("2 boletos").</summary>
    [ObservableProperty]
    private string boletosTexto = string.Empty;

    // ── F6: estado de impresión ────────────────────────────────────

    /// <summary>True mientras se envía el comprobante a la impresora.</summary>
    [ObservableProperty]
    private bool imprimiendo;

    /// <summary>Mensaje de resultado de la impresión (éxito o error).</summary>
    [ObservableProperty]
    private string mensajeImpresion = string.Empty;

    /// <summary>True cuando el fallo permite seleccionar otra impresora.</summary>
    [ObservableProperty]
    private bool requiereSeleccionImpresora;

    /// <summary>True cuando hay un mensaje de impresión visible.</summary>
    [ObservableProperty]
    private bool hayMensajeImpresion;

    /// <summary>True cuando la impresión fue exitosa (muestra ACEPTAR).</summary>
    [ObservableProperty]
    private bool impresionExitosa;

    /// <summary>Comprobante de la última venta (reintento sin repetir venta).</summary>
    private ComprobanteLotenal? _comprobante;

    public VentaExitosaLotenalViewModel(IImpresoraService impresora)
    {
        _impresora = impresora;
        Title = "Venta Exitosa";
    }

    /// <summary>
    /// Botón "Continuar": limpia la venta en curso (SesionVentaLotenal)
    /// y regresa a la pestaña Vender (Home). El Home es un ShellContent
    /// del TabBar SIN ruta global: GoToAsync("//HomePage") CRASHEA
    /// (ArgumentException, pitfall documentado) — se regresa con pops
    /// relativos hasta la pestaña (misma solución que CapturaPremios).
    /// </summary>
    [RelayCommand]
    private async Task ContinuarAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            Data.SesionVentaLotenal.Limpiar();
            _comprobante = null;

            if (Shell.Current is not null)
            {
                // Stack: Home → SorteosLotenal → SorteosActivos →
                // AgregarBoletos → Carrito → VentaExitosa (5 pushes).
                await Shell.Current.GoToAsync("../../../../..");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ── F6: impresión del comprobante ───────────────────────────────

    /// <summary>
    /// "Imprimir comprobante": arma el ComprobanteLotenal con los datos
    /// REALES de la venta (folio/total de la respuesta + dotación y
    /// desglose de la sesión de venta aún viva) y lo envía por
    /// Bluetooth. Un FALLO no repite la venta: se muestra el error con
    /// Reintentar/Seleccionar impresora.
    /// </summary>
    [RelayCommand]
    private async Task ImprimirComprobanteAsync()
    {
        if (Imprimiendo || IsBusy)
        {
            return;
        }

        // El comprobante se arma UNA vez con los datos vivos de la sesión;
        // los reintentos reutilizan el mismo objeto (la venta ya ocurrió).
        if (_comprobante is null)
        {
            _comprobante = ComprobanteLotenalFactory.DesdeVenta(
                Folio,
                ParseTotal(TotalTexto),
                ParseBoletos(BoletosTexto));
        }

        Imprimiendo = true;
        try
        {
            ResultadoImpresion r = await _impresora.ImprimirComprobanteAsync(_comprobante);
            MensajeImpresion = r.Mensaje;
            RequiereSeleccionImpresora = !r.Exito;
            ImpresionExitosa = r.Exito;
            HayMensajeImpresion = true;
        }
        finally
        {
            Imprimiendo = false;
        }
    }

    /// <summary>Oculta el mensaje de impresión.</summary>
    [RelayCommand]
    private void CerrarMensajeImpresion()
    {
        HayMensajeImpresion = false;
    }

    /// <summary>Abre la selección de impresora (F6 ImpresoraPage).</summary>
    [RelayCommand]
    private async Task SeleccionarImpresoraAsync()
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync(nameof(Views.ImpresoraPage));
        }
    }

    // ── Parseo de los QueryProperties ──────────────────────────────

    private static decimal ParseTotal(string texto)
    {
        // "$140.00" → 140.00 (el total llega formateado del carrito).
        string limpio = texto.Trim().TrimStart('$');
        return decimal.TryParse(limpio, out decimal v) ? v : 0m;
    }

    private static IReadOnlyList<int> ParseBoletos(string texto)
    {
        // "2 boletos" → 2 id_billete ficticios 1..N: los ids EXACTOS no
        // viajan por QueryProperty; el desglose de la sesión (viva)
        // provee las series reales. Este conteo es solo para el caso
        // en que la sesión ya no esté (línea resumen).
        int n = int.TryParse(new string(texto.TakeWhile(char.IsDigit).ToArray()), out int k) ? k : 0;
        return Enumerable.Range(1, Math.Max(0, n)).ToList();
    }
}
