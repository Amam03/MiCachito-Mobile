using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Services.Scanning;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla de escaneo (mockup 2: Consulta de premio - escaneo).
/// Flujo: la camara integrada (CameraScannerView) escanea el QR del boleto en
/// vivo (o entra por captura manual, via de prueba en emulador), se parsea con
/// las reglas del desktop y se navega a la pantalla 2.1 con el resultado.
/// </summary>
public partial class ConsultaPremiosViewModel : BaseViewModel
{
    private readonly ConsultaPremiosService _consultaService;

    /// <summary>Codigo tecleado a mano (via de prueba en emulador).</summary>
    [ObservableProperty]
    private string codigoManual = string.Empty;

    /// <summary>Aviso de error (codigo invalido / permiso negado); vacio = sin error.</summary>
    [ObservableProperty]
    private string aviso = string.Empty;

    /// <summary>True cuando el permiso de camara fue concedido y el preview puede arrancar.</summary>
    [ObservableProperty]
    private bool camaraLista;

    /// <summary>Inverso de CamaraLista para el mensaje de permiso denegado.</summary>
    [ObservableProperty]
    private bool permisoDenegado;

    /// <summary>True = linterna encendida (escaneo en fisico con poca luz).</summary>
    [ObservableProperty]
    private bool linternaEncendida;

    public ConsultaPremiosViewModel(ConsultaPremiosService consultaService)
    {
        _consultaService = consultaService;
        Title = "Consulta de Premios";
    }

    /// <summary>
    /// Permisos: se piden al APARECER la pantalla (antes de crear el control
    /// de camara). Si se niegan, queda la entrada manual como alternativa.
    /// </summary>
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
                Aviso = "Se necesita permiso de camara para escanear el boleto. Puedes capturar el codigo a mano.";
            }
        }
        catch (Exception)
        {
            CamaraLista = false;
            PermisoDenegado = true;
        }
    }

    /// <summary>
    /// Procesa una cadena cruda: la parsea y, si es valida, navega a la
    /// pantalla de resultado (2.1). Compartido por escaneo en vivo y manual.
    /// </summary>
    public async Task ProcesarCadenaAsync(string cadena)
    {
        BilleteParseado? parseado = BilleteParser.ParsearCadena(cadena);
        if (parseado is null)
        {
            Aviso = "Codigo no reconocido. Verifica el boleto e intenta de nuevo.";
            return;
        }

        Aviso = string.Empty;

        // Limpiar el campo: al volver del resultado (Aceptar) el escaner debe
        // quedar listo para capturar otro boleto sin arrastrar el codigo previo.
        CodigoManual = string.Empty;

        _ = _consultaService.Consultar(parseado);

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(Views.ResultadoConsultaPremiosPage)}?codigo={Uri.EscapeDataString(parseado.CodigoCompleto)}");
        }
    }

    /// <summary>Recibe el texto decodificado por la camara integrada (comando).</summary>
    [RelayCommand]
    private async Task CodigoDetectadoAsync(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        texto = texto.Trim();

        // ignorar el 2o QR de los boletos (URL cartilla de derechos)
        if (!texto.All(char.IsDigit))
        {
            return;
        }

        await ProcesarCadenaAsync(texto);
    }

    [RelayCommand]
    private Task ConsultarManualAsync()
    {
        if (string.IsNullOrWhiteSpace(CodigoManual))
        {
            Aviso = "Captura el codigo del boleto o escanea su QR.";
            return Task.CompletedTask;
        }

        // Leer y limpiar SIEMPRE (valido o invalido): el campo queda listo para
        // el siguiente boleto sin arrastrar texto previo. Trim: algunos IMEs
        // dejan saltos de linea o espacios al final al pulsar la tecla Done.
        string valor = CodigoManual.Trim();
        CodigoManual = string.Empty;
        return ProcesarCadenaAsync(valor);
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

    /// <summary>Al salir de la pantalla: apagar camara y linterna.</summary>
    public void AlDesaparecer()
    {
        CamaraLista = false;
        LinternaEncendida = false;
    }

    /// <summary>Linterna on/off (para escanear con poca luz en un fisico).</summary>
    [RelayCommand]
    private void ToggleLinterna()
    {
        LinternaEncendida = !LinternaEncendida;
    }

    /// <summary>Pulso para decodificar el frame actual (boton Capturar QR).</summary>
    [ObservableProperty]
    private bool forzarCaptura;

    /// <summary>Boton principal: capturar/decodificar el QR del boleto YA.</summary>
    [RelayCommand]
    private void CapturarQr()
    {
        if (!CamaraLista)
        {
            Aviso = "Se necesita permiso de camara para escanear. Puedes capturar el codigo a mano.";
            return;
        }
        Aviso = string.Empty;
        ForzarCaptura = true; // pulso: el handler lo consume en el proximo frame
    }

    /// <summary>Una captura forzada no encontro ningun QR en el frame.</summary>
    [RelayCommand]
    private void SinDeteccion()
    {
        Aviso = "No se detecto ningun codigo. Acerca el boleto al marco e intentalo de nuevo.";
    }
}
