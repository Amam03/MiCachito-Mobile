using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de "Escanear Series" (mockup 6.2 escaneo): reutiliza el
/// componente de cámara/escáner de Premios y Reintegros (mismo diseño:
/// marco, linterna) + entrada manual (vía de prueba en emulador) + banner
/// de error de fecha del mockup. Cada código aceptado se registra en el
/// modo activo de la devolución en curso. Fase solo-interfaz.
/// </summary>
public partial class EscanearSeriesViewModel : BaseViewModel
{
    private readonly DevolucionService _devoluciones;

    /// <summary>Modo activo fijado al navegar (se lee del servicio singleton).</summary>
    private ModoCapturaDevolucion ModoActual => _devoluciones.ModoActivo;

    /// <summary>Código tecleado a mano (vía de prueba en emulador).</summary>
    [ObservableProperty]
    private string codigoManual = string.Empty;

    /// <summary>Aviso de error (código inválido / duplicado / fecha); vacío = nada.</summary>
    [ObservableProperty]
    private string aviso = string.Empty;

    /// <summary>True cuando el permiso de cámara fue concedido.</summary>
    [ObservableProperty]
    private bool camaraLista;

    /// <summary>Inverso de CamaraLista para el mensaje de permiso denegado.</summary>
    [ObservableProperty]
    private bool permisoDenegado;

    /// <summary>True = linterna encendida.</summary>
    [ObservableProperty]
    private bool linternaEncendida;

    /// <summary>Título del header: "Escanear [Modo]" según el modo activo.</summary>
    public string TituloModo =>
        _devoluciones.ModoActivo == ModoCapturaDevolucion.Cachitos
            ? "Escanear Cachitos"
            : _devoluciones.ModoActivo == ModoCapturaDevolucion.Tiras
                ? "Escanear Tiras"
                : "Escanear Series";

    /// <summary>Modo activo legible (bajo el título).</summary>
    public string ModoText => _devoluciones.ModoActivo switch
    {
        ModoCapturaDevolucion.Cachitos => "Cachitos",
        ModoCapturaDevolucion.Tiras => "Tiras",
        _ => "Series",
    };

    /// <summary>True cuando hay aviso (para el banner rojo).</summary>
    public bool HayAviso => !string.IsNullOrEmpty(Aviso);

    /// <summary>Acumulado visible bajo la entrada manual.</summary>
    public string AcumuladoText =>
        $"Cachitos: {_devoluciones.ContadorCachitos:00}   Tiras: {_devoluciones.ContadorTiras:00}   Series: {_devoluciones.ContadorSeries:00}";

    public EscanearSeriesViewModel(DevolucionService devoluciones)
    {
        _devoluciones = devoluciones;
        Title = "Escanear Series";
    }

    /// <summary>Permisos al APARECER (antes de crear el control de cámara).</summary>
    public async Task AlAparecerAsync()
    {
        try
        {
            PermissionStatus permiso = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (permiso != PermissionStatus.Granted)
            {
                permiso = await Permissions.RequestAsync<Permissions.Camera>();
            }
            CamaraLista = permiso == PermissionStatus.Granted;
            PermisoDenegado = !CamaraLista;
            if (!CamaraLista)
            {
                Aviso = "Se necesita permiso de cámara para escanear. Puedes capturar el código a mano.";
            }
        }
        catch (Exception)
        {
            CamaraLista = false;
            PermisoDenegado = true;
        }

        OnPropertyChanged(nameof(ModoText));
        OnPropertyChanged(nameof(TituloModo));
        OnPropertyChanged(nameof(AcumuladoText));
    }

    /// <summary>Fija el modo activo (lo llama Nueva Devolución antes de navegar).</summary>
    public void FijarModo(ModoCapturaDevolucion modo)
    {
        _devoluciones.FijarModo(modo);
    }

    /// <summary>Cadena cruda (cámara o manual): registra en el modo activo.</summary>
    public void ProcesarCadena(string cadena)
    {
        if (string.IsNullOrWhiteSpace(cadena))
        {
            return;
        }

        string valor = cadena.Trim();

        // Ignorar el 2º QR de los boletos (URL cartilla de derechos).
        if (!valor.All(char.IsDigit))
        {
            return;
        }

        (bool ok, string mensaje) = _devoluciones.Capturar(valor, ModoActual);
        Aviso = ok ? string.Empty : mensaje;
        OnPropertyChanged(nameof(HayAviso));
        if (ok)
        {
            CodigoManual = string.Empty;
        }

        OnPropertyChanged(nameof(AcumuladoText));
    }

    /// <summary>Recibe el texto decodificado por la cámara (comando del control).</summary>
    [RelayCommand]
    private void CodigoDetectado(string texto)
    {
        ProcesarCadena(texto ?? string.Empty);
    }

    [RelayCommand]
    private void ConsultarManual()
    {
        if (string.IsNullOrWhiteSpace(CodigoManual))
        {
            Aviso = "Captura el código del boleto o escanea su QR.";
            OnPropertyChanged(nameof(HayAviso));
            return;
        }

        // Leer y limpiar SIEMPRE: evita concatenaciones.
        string valor = CodigoManual.Trim();
        ProcesarCadena(valor);
    }

    /// <summary>Regresa a Nueva Devolución.</summary>
    [RelayCommand]
    private Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }
        return Task.CompletedTask;
    }

    /// <summary>Al salir: apagar cámara y linterna.</summary>
    public void AlDesaparecer()
    {
        CamaraLista = false;
        LinternaEncendida = false;
    }

    /// <summary>Linterna on/off.</summary>
    [RelayCommand]
    private void ToggleLinterna()
    {
        LinternaEncendida = !LinternaEncendida;
    }
}
