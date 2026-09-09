using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

#if ANDROID
using MiCachito.Mobile.Platforms.Android.Services;
#endif

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM del "Detalle de Pago" (mockups 8.1/8.2): header azul con Folio,
/// Fecha y Total, desglose Descripción/Referencia/Monto y FAB de
/// descarga con overlay "Descargando..." + aviso verde de guardado.
/// Fase solo-interfaz: la descarga se simula (~2 s) y el PDF se genera
/// de verdad con SkiaSharp.
/// </summary>
public partial class DetallePagoViewModel : ObservableObject, IQueryAttributable
{
    private readonly RecibosPagoService _recibos;
    private readonly ReciboPagoPdfService _pdf;

    private ReciboPago? _recibo;

    /// <summary>Folio recibido por query string (patrón del proyecto).</summary>
    public string? FolioQuery { get; set; }

    [ObservableProperty]
    private bool _descargando;

    [ObservableProperty]
    private bool _descargaExitosa;

    [ObservableProperty]
    private string? _rutaPdf;

    public ReciboPago? Recibo => _recibo;

    public string Title => "Detalles de Pago";

    /// <summary>True cuando hay un recibo cargado (controla el FAB).</summary>
    public bool HayRecibo => _recibo is not null;

    /// <summary>Regresa a la lista de recibos.</summary>
    [RelayCommand]
    private Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }
        return Task.CompletedTask;
    }

    public DetallePagoViewModel(RecibosPagoService recibos, ReciboPagoPdfService pdf)
    {
        _recibos = recibos;
        _pdf = pdf;
    }

    /// <summary>Carga el recibo por folio (patrón AlAparecer + QueryProperty).</summary>
    public void AlAparecer()
    {
        int folio = int.TryParse(FolioQuery, out int f) ? f : 0;
        _recibo = _recibos.ObtenerPorFolio(folio);
        OnPropertyChanged(nameof(Recibo));
        OnPropertyChanged(nameof(HayRecibo));
        Descargando = false;
        DescargaExitosa = false;
    }

    void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("folio", out object? v) && v is string s)
        {
            FolioQuery = s;
        }
    }

    /// <summary>
    /// Descarga el comprobante: overlay "Descargando..." (simulado ~2 s,
    /// mockup 8.2), genera el PDF real, lo copia a Descargas y muestra
    /// el aviso verde "Archivo guardado en la carpeta de Descargas".
    /// </summary>
    [RelayCommand]
    private async Task DescargarAsync()
    {
        if (Descargando || _recibo is null)
        {
            return;
        }

        Descargando = true;
        DescargaExitosa = false;
        try
        {
            // Generación real del PDF
            string ruta = await _pdf.GenerarAsync(_recibo);
            RutaPdf = ruta;

            // Simulación del proceso de descarga (solo UI)
            await Task.Delay(2000);

#if ANDROID
            string? guardado = DescargasService.GuardarEnDescargas(
                ruta, _recibo.NombreArchivoPdf + ".pdf");
            DescargaExitosa = guardado is not null;
#else
            DescargaExitosa = true;
#endif
        }
        finally
        {
            Descargando = false;
        }
    }

    /// <summary>Abre/visualiza el comprobante con el visor del sistema.</summary>
    [RelayCommand]
    private async Task AbrirComprobanteAsync()
    {
        if (string.IsNullOrEmpty(RutaPdf) || !System.IO.File.Exists(RutaPdf))
        {
            return;
        }

        try
        {
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(RutaPdf),
                Title = "Comprobante de Pago",
            });
        }
        catch
        {
            // Fallback: diálogo de compartir si no hay visor (emulador)
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Comprobante de Pago",
                File = new ShareFile(RutaPdf),
            });
        }
    }
}
