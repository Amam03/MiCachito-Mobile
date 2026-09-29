using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de "Nueva Devolución" (mockup 6.2): sorteo en verde, 3 modos
/// de captura (Series/Tiras/Cachitos), botón Escanear, contadores con
/// flecha al desglose, Guardar (registra y regresa) y Cancelar con
/// confirmación de mantener presionado (mockup 6.2 Cancelar).
/// Fase solo-interfaz: contadores solo cambian con capturas reales.
/// </summary>
public partial class NuevaDevolucionViewModel : BaseViewModel
{
    private readonly DevolucionService _devoluciones;

    /// <summary>Impresión del comprobante (F6); la devolución NO se repite.</summary>
    private readonly Services.IImpresoraService _impresora;

    // ── F6: estado de impresión del comprobante de devolución ─────

    /// <summary>Diálogo post-guardado con "Imprimir comprobante" (F6).</summary>
    [ObservableProperty]
    private bool dialogoDevolucionExitosa;

    /// <summary>Mensaje de resultado de impresión (éxito/error).</summary>
    [ObservableProperty]
    private string mensajeImpresion = string.Empty;

    /// <summary>True mientras se envía el comprobante a la impresora.</summary>
    [ObservableProperty]
    private bool imprimiendoComprobante;

    /// <summary>True cuando hay un mensaje de impresión visible.</summary>
    [ObservableProperty]
    private bool hayMensajeImpresion;

    /// <summary>Comprobante de la última devolución (reintento sin repetir devolución).</summary>
    private Services.ComprobanteLotenal? _comprobanteDevolucion;

    /// <summary>Modo de captura activo (seleccionado entre los 3).</summary>
    [ObservableProperty]
    private ModoCapturaDevolucion modoActivo = ModoCapturaDevolucion.Series;

    /// <summary>Nombre del sorteo en verde bajo el título (vacío si no hay).</summary>
    [ObservableProperty]
    private string nombreSorteo = string.Empty;

    /// <summary>Diálogo "Atención: Mantén presionado el botón para cancelar".</summary>
    [ObservableProperty]
    private bool avisoCancelarVisible;

    /// <summary>True mientras se envía la devolución al backend (F4).</summary>
    [ObservableProperty]
    private bool guardando;

    /// <summary>Mensaje de error del envío (aviso rojo, la devolución se conserva).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True cuando hay error visible.</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>Cachitos capturados (filas en curso, modo Cachitos).</summary>
    public int ContadorCachitos => _devoluciones.ContadorCachitos;

    /// <summary>Tiras capturadas (filas en curso, modo Tiras).</summary>
    public int ContadorTiras => _devoluciones.ContadorTiras;

    /// <summary>Series capturadas (filas en curso, modo Series).</summary>
    public int ContadorSeries => _devoluciones.ContadorSeries;

    /// <summary>Header "Nueva Devolución - N" del servicio.</summary>
    public string Titulo => _devoluciones.TituloEnCurso;

    public NuevaDevolucionViewModel(
        DevolucionService devoluciones,
        Services.IImpresoraService impresora)
    {
        _devoluciones = devoluciones;
        _impresora = impresora;
        Title = "Nueva Devolución";
    }

    /// <summary>Carga el sorteo en curso al aparecer.</summary>
    public void AlAparecer()
    {
        NombreSorteo = _devoluciones.SorteoEnCurso is null
            ? string.Empty
            : string.IsNullOrEmpty(_devoluciones.SorteoEnCurso.NumeroSorteo)
                ? _devoluciones.SorteoEnCurso.NombreProducto
                : $"{_devoluciones.SorteoEnCurso.NombreProducto} {_devoluciones.SorteoEnCurso.NumeroSorteo}";
        OnPropertyChanged(nameof(Titulo));
        NotificarContadores();
    }

    private void NotificarContadores()
    {
        OnPropertyChanged(nameof(ContadorCachitos));
        OnPropertyChanged(nameof(ContadorTiras));
        OnPropertyChanged(nameof(ContadorSeries));
    }

    /// <summary>Modo activo cambia (tap en Series/Tiras/Cachitos).</summary>
    partial void OnModoActivoChanged(ModoCapturaDevolucion value)
    {
        OnPropertyChanged(nameof(EsSeries));
        OnPropertyChanged(nameof(EsTiras));
        OnPropertyChanged(nameof(EsCachitos));
        OnPropertyChanged(nameof(BgSeries));
        OnPropertyChanged(nameof(BgTiras));
        OnPropertyChanged(nameof(BgCachitos));
        OnPropertyChanged(nameof(TextoSeries));
        OnPropertyChanged(nameof(TextoTiras));
        OnPropertyChanged(nameof(TextoCachitos));
    }

    public bool EsSeries => ModoActivo == ModoCapturaDevolucion.Series;
    public bool EsTiras => ModoActivo == ModoCapturaDevolucion.Tiras;
    public bool EsCachitos => ModoActivo == ModoCapturaDevolucion.Cachitos;

    /// <summary>Fondo del botón de modo (activo morado / inactivo gris claro), patrón ColorHex.</summary>
    public string BgSeries => EsSeries ? "#4125F4" : "#E8E8F5";
    public string BgTiras => EsTiras ? "#4125F4" : "#E8E8F5";
    public string BgCachitos => EsCachitos ? "#4125F4" : "#E8E8F5";

    /// <summary>Texto del botón de modo (activo blanco / inactivo oscuro).</summary>
    public string TextoSeries => EsSeries ? "#FFFFFF" : "#1A1A1A";
    public string TextoTiras => EsTiras ? "#FFFFFF" : "#1A1A1A";
    public string TextoCachitos => EsCachitos ? "#FFFFFF" : "#1A1A1A";

    /// <summary>Selecciona un modo de captura.</summary>
    [RelayCommand]
    private void SeleccionarModo(string modo)
    {
        if (Enum.TryParse<ModoCapturaDevolucion>(modo, out ModoCapturaDevolucion m))
        {
            ModoActivo = m;
        }
    }

    /// <summary>ESCONEAR: fija el modo activo y abre la pantalla de escaneo.</summary>
    [RelayCommand]
    private Task EscanearAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        _devoluciones.FijarModo(ModoActivo);
        return Shell.Current.GoToAsync(nameof(Views.EscanearSeriesPage));
    }

    /// <summary>Contador con flecha: abre el desglose del modo dado.</summary>
    [RelayCommand]
    private Task AbrirDesgloseAsync(string modo)
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync($"{nameof(Views.DesgloseDevolucionPage)}?modo={modo}");
    }

    /// <summary>
    /// GUARDAR (F4, REAL): envía los boletos capturados al backend
    /// (POST api/mobile/ventas/devolver — movimiento pendiente con
    /// estado LIVE inmediato) y regresa al listado. Sin capturas no
    /// hace nada (el botón requiere al menos un boleto). Errores
    /// (409 vendido/devuelto/dotación cerrada, 403 ajeno, red): aviso
    /// rojo con el mensaje del backend; la devolución se CONSERVA
    /// para corregir y reintentar.
    /// </summary>
    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Shell.Current is null || IsBusy || Guardando)
        {
            return;
        }

        if (ContadorCachitos + ContadorTiras + ContadorSeries == 0)
        {
            return;
        }

        Guardando = true;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(HayError));
        try
        {
            var registro = await _devoluciones.GuardarAsync().ConfigureAwait(true);

            // F6: comprobante de la devolución recién creada (folio MA-
            // real, sorteo y filas) — se arma UNA vez; reintentos de
            // impresión reutilizan el objeto sin repetir la devolución.
            _comprobanteDevolucion = Services.ComprobanteLotenalFactory.DesdeDevolucion(registro);

            // F6: diálogo de éxito con opción Imprimir (la devolución YA
            // está registrada en backend; la impresión es best-effort).
            DialogoDevolucionExitosa = true;
        }
        catch (TaskCanceledException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (OperationCanceledException)
        {
            MensajeError = "Devolución cancelada";
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
            Guardando = false;
        }
    }

    /// <summary>
    /// Tap en Cancelar: solo muestra el aviso del mockup ("Mantén
    /// presionado el botón para cancelar"). La cancelación real requiere
    /// mantener presionado (CancelarMantenido).
    /// </summary>
    [RelayCommand]
    private void Cancelar()
    {
        AvisoCancelarVisible = true;
    }

    /// <summary>Mantener presionado Cancelar (~2.5 s): cancela de verdad y regresa.</summary>
    [RelayCommand]
    private Task CancelarMantenidoAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        _devoluciones.Cancelar();
        return Shell.Current.GoToAsync("../..");
    }

    /// <summary>ACEPTAR del aviso: cierra el diálogo (sigue en la devolución).</summary>
    [RelayCommand]
    private void AceptarAviso()
    {
        AvisoCancelarVisible = false;
    }

    /// <summary>Al volver del escaneo: refresca contadores.</summary>
    public void AlVolverDeEscaneo()
    {
        NotificarContadores();
    }

    // ── F6: impresión del comprobante de devolución ──────────────────

    /// <summary>
    /// "Imprimir comprobante" del diálogo de éxito: envía el comprobante
    /// de la devolución YA registrada (folio MA- real) por Bluetooth
    /// SPP/ESC-POS. Un fallo NO repite la devolución: muestra el error
    /// con Reintentar (reusa _comprobanteDevolucion).
    /// </summary>
    [RelayCommand]
    private async Task ImprimirComprobanteAsync()
    {
        if (ImprimiendoComprobante || _comprobanteDevolucion is null)
        {
            return;
        }

        ImprimiendoComprobante = true;
        MensajeImpresion = string.Empty;
        HayMensajeImpresion = false;
        try
        {
            ResultadoImpresion r = await _impresora.ImprimirComprobanteAsync(_comprobanteDevolucion);
            MensajeImpresion = r.Mensaje;
            HayMensajeImpresion = true;
        }
        finally
        {
            ImprimiendoComprobante = false;
        }
    }

    /// <summary>
    /// "Continuar" del diálogo de éxito: cierra el diálogo y regresa al
    /// listado de devoluciones (flujo original tras guardar).
    /// </summary>
    [RelayCommand]
    private async Task ContinuarDevolucionAsync()
    {
        if (Shell.Current is null)
        {
            return;
        }

        DialogoDevolucionExitosa = false;
        _comprobanteDevolucion = null;
        await Shell.Current.GoToAsync("../..");
    }
}
