using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM de Reportes (mockups 9.x). Pestañas: Estado de Cuenta (activa),
/// Fondo de Ahorro y Facturación (solo navegación/placeholder).
/// Fase SOLO INTERFAZ: el reporte se genera con valores en cero y SIN
/// sorteos (ni mocks ni seeds — directriz del usuario); la descarga
/// genera el PDF real (estado-cuenta-&lt;fecha&gt;.pdf) y lo guarda en
/// Descargas.
/// </summary>
public partial class ReportesViewModel : BaseViewModel
{
    private readonly EstadoCuentaService _servicio;
    private readonly EstadoDeCuentaPdfService _pdf;

    /// <summary>Reporte generado (null hasta generar).</summary>
    private EstadoCuenta? _estadoCuenta;

    /// <summary>Pestaña activa (0=Estado de Cuenta, 1=Fondo de Ahorro, 2=Facturación).</summary>
    [ObservableProperty]
    private int _pestanaActiva;

    /// <summary>True mientras corre "Procesando....".</summary>
    [ObservableProperty]
    private bool _procesando;

    /// <summary>True mientras corre "Descargando....".</summary>
    [ObservableProperty]
    private bool _descargando;

    /// <summary>True cuando el reporte ya fue generado en pantalla.</summary>
    [ObservableProperty]
    private bool _reporteGenerado;

    /// <summary>True tras completar la descarga (muestra el FAB PDF).</summary>
    [ObservableProperty]
    private bool _reporteDescargado;

    /// <summary>Ruta absoluta del PDF generado.</summary>
    [ObservableProperty]
    private string? _rutaPdf;

    public ReportesViewModel(EstadoCuentaService servicio, EstadoDeCuentaPdfService pdf)
    {
        _servicio = servicio;
        _pdf = pdf;
        Title = "Reportes";
    }

    // ── Propiedades derivadas (patrón del proyecto: sin converters) ──

    /// <summary>Reporte en pantalla (null antes de generar).</summary>
    public EstadoCuenta? EstadoCuenta
    {
        get => _estadoCuenta;
        private set => SetProperty(ref _estadoCuenta, value);
    }

    /// <summary>True cuando hay reporte en pantalla.</summary>
    public bool HayReporte => _estadoCuenta is not null && ReporteGenerado;

    /// <summary>True cuando NO hay reporte (muestra el botón Generar).</summary>
    public bool SinReporte => !HayReporte;

    /// <summary>True si la pestaña Estado de Cuenta está activa.</summary>
    public bool Pestana0Activa => PestanaActiva == 0;

    /// <summary>True si la pestaña Fondo de Ahorro está activa.</summary>
    public bool Pestana1Activa => PestanaActiva == 1;

    /// <summary>True si la pestaña Facturación está activa.</summary>
    public bool Pestana2Activa => PestanaActiva == 2;

    /// <summary>Placeholder de las pestañas 2 y 3.</summary>
    public bool PestanaPlaceholder => PestanaActiva != 0;

    /// <summary>Nombre de la pestaña activa (título del placeholder).</summary>
    public string TituloPestana => PestanaActiva switch
    {
        1 => "Fondo de Ahorro",
        2 => "Facturación",
        _ => "Estado de Cuenta",
    };

    /// <summary>Iconos de las pestañas (azul la activa, gris las demás).</summary>
    public string IconoPestana0 => Pestana0Activa ? "icon_grafica_azul.svg" : "icon_grafica_gris.svg";
    public string IconoPestana1 => Pestana1Activa ? "icon_alcancia_azul.svg" : "icon_alcancia_gris.svg";
    public string IconoPestana2 => Pestana2Activa ? "icon_recibo_azul.svg" : "icon_recibo_gris.svg";

    /// <summary>Color del label de cada pestaña (azul la activa, gris las demás).</summary>
    public string ColorPestana0 => Pestana0Activa ? "#4125F4" : "#9E9E9E";
    public string ColorPestana1 => Pestana1Activa ? "#4125F4" : "#9E9E9E";
    public string ColorPestana2 => Pestana2Activa ? "#4125F4" : "#9E9E9E";

    /// <summary>True si el listado de sorteos está vacío (fase actual: siempre).</summary>
    public bool SinSorteos => EstadoCuenta?.Sorteos.Count == 0;

    // ── Valores planos del reporte (bindings compilados: decimal, no anidados) ──

    public decimal FondoDeAhorro => EstadoCuenta?.FondoDeAhorro ?? 0m;
    public decimal Fideicomiso => EstadoCuenta?.Fideicomiso ?? 0m;
    public decimal Pagares => EstadoCuenta?.Pagares ?? 0m;
    public decimal BolsaElectronica => EstadoCuenta?.BolsaElectronica ?? 0m;
    public decimal GarantiaTotal => EstadoCuenta?.GarantiaTotal ?? 0m;
    public decimal CapacidadDeCredito => EstadoCuenta?.CapacidadDeCredito ?? 0m;
    public decimal VencidoTotal => EstadoCuenta?.VencidoTotal ?? 0m;
    public decimal ConsignaTotal => EstadoCuenta?.ConsignaTotal ?? 0m;
    public decimal PagosTotal => EstadoCuenta?.PagosTotal ?? 0m;
    public decimal TotalFinal => EstadoCuenta?.TotalFinal ?? 0m;

    partial void OnPestanaActivaChanged(int value)
    {
        OnPropertyChanged(nameof(Pestana0Activa));
        OnPropertyChanged(nameof(Pestana1Activa));
        OnPropertyChanged(nameof(Pestana2Activa));
        OnPropertyChanged(nameof(PestanaPlaceholder));
        OnPropertyChanged(nameof(TituloPestana));
        OnPropertyChanged(nameof(IconoPestana0));
        OnPropertyChanged(nameof(IconoPestana1));
        OnPropertyChanged(nameof(IconoPestana2));
        OnPropertyChanged(nameof(ColorPestana0));
        OnPropertyChanged(nameof(ColorPestana1));
        OnPropertyChanged(nameof(ColorPestana2));
    }

    partial void OnReporteGeneradoChanged(bool value)
    {
        OnPropertyChanged(nameof(HayReporte));
        OnPropertyChanged(nameof(SinReporte));
        OnPropertyChanged(nameof(SinSorteos));
        foreach (string n in new[]
        {
            nameof(FondoDeAhorro), nameof(Fideicomiso), nameof(Pagares),
            nameof(BolsaElectronica), nameof(GarantiaTotal), nameof(CapacidadDeCredito),
            nameof(VencidoTotal), nameof(ConsignaTotal), nameof(PagosTotal), nameof(TotalFinal),
        })
        {
            OnPropertyChanged(n);
        }
    }

    /// <summary>
    /// Genera el reporte: overlay "Procesando...." (~2 s simulados) y
    /// estructura en cero sin sorteos.
    /// </summary>
    [RelayCommand]
    private async Task GenerarReporteAsync()
    {
        if (Procesando || ReporteGenerado)
        {
            return;
        }

        Procesando = true;
        try
        {
            EstadoCuenta = await _servicio.ObtenerAsync();
            await Task.Delay(2000);
            ReporteGenerado = true;
            OnPropertyChanged(nameof(HayReporte));
            OnPropertyChanged(nameof(SinReporte));
        }
        finally
        {
            Procesando = false;
        }
    }

    /// <summary>
    /// Descarga el reporte: overlay "Descargando...." (~2 s), genera el
    /// PDF real, lo copia a Descargas (MediaStore) y muestra el FAB PDF.
    /// </summary>
    [RelayCommand]
    private async Task DescargarAsync()
    {
        if (Descargando || EstadoCuenta is null)
        {
            return;
        }

        Descargando = true;
        try
        {
            string ruta = await _pdf.GenerarAsync(EstadoCuenta);
            RutaPdf = ruta;

            await Task.Delay(2000);

#if ANDROID
            string nombre = $"estado-cuenta-{EstadoCuenta.FechaEmision:dd-MM-yyyy}.pdf";
            Platforms.Android.Services.DescargasService.GuardarEnDescargas(ruta, nombre);
#endif
            ReporteDescargado = true;
        }
        finally
        {
            Descargando = false;
        }
    }

    /// <summary>Abre el PDF con el visor del sistema (fallback Share).</summary>
    [RelayCommand]
    private async Task AbrirPdfAsync()
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
                Title = "Estado de Cuenta",
            });
        }
        catch
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Estado de Cuenta",
                File = new ShareFile(RutaPdf),
            });
        }
    }

    /// <summary>Cambia de pestaña desde el tab bar interno.</summary>
    [RelayCommand]
    private void CambiarPestana(string indice)
    {
        if (int.TryParse(indice, out int i) && i >= 0 && i <= 2)
        {
            PestanaActiva = i;
        }
    }

    /// <summary>Regresa al menú de Gestión.</summary>
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
