using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;
using MiCachito.Mobile.Services.Scanning;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel del escaner continuo (mockup 3.3 escaneo): reutiliza la camara
/// integrada (CameraScannerView + zxing-cpp) y el BilleteParser del desktop.
///
/// Flujo (regla del usuario): el escaneo es CONTINUO — cada boleto detectado
/// muestra el dialogo "Atencion: EL CACHITO TIENE/NO TIENE PREMIO (REINTEGRO)"
/// con ACEPTAR que lo agrega al acumulado y REGRESA AL ESCANER para seguir
/// capturando hasta que el usuario salga atras; nada se pierde al navegar
/// (el acumulado vive en PremiosReintegrosService).
/// Entrada manual preparada (via de prueba en emulador), igual que Consulta
/// de Premios.
/// </summary>
public partial class EscanearBoletosViewModel : BaseViewModel
{
    private readonly PremiosReintegrosService _premiosService;
    private readonly ConsultaPremiosService _consultaService;

    /// <summary>Codigo tecleado a mano (via de prueba en emulador).</summary>
    [ObservableProperty]
    private string codigoManual = string.Empty;

    /// <summary>Aviso de error (codigo invalido / duplicado / permiso); vacio = nada.</summary>
    [ObservableProperty]
    private string aviso = string.Empty;

    /// <summary>True cuando el permiso de camara fue concedido.</summary>
    [ObservableProperty]
    private bool camaraLista;

    /// <summary>Inverso de CamaraLista para el mensaje de permiso denegado.</summary>
    [ObservableProperty]
    private bool permisoDenegado;

    /// <summary>True = linterna encendida.</summary>
    [ObservableProperty]
    private bool linternaEncendida;

    /// <summary>Dialogo de resultado visible (mockup 3.3 escaneo).</summary>
    [ObservableProperty]
    private bool dialogoVisible;

    [ObservableProperty]
    private string mensajeDialogo = string.Empty;

    /// <summary>Color del boton ACEPTAR segun resultado (patron ColorHex con #).</summary>
    [ObservableProperty]
    private string colorAceptar = "#FF5353";

    /// <summary>Detalle del boleto en espera de ACEPTAR (null = sin dialogo).</summary>
    private BoletoCapturado? _boletoPendiente;

    /// <summary>Ignora el toque del scrim ~600 ms tras abrir (tap fantasma).</summary>
    private long _dialogoAbiertoEnUtcMs;

    /// <summary>Acumulado visible bajo la entrada manual (escaneo continuo).</summary>
    public string AcumuladoText =>
        $"Premios: {_premiosService.ContadorPremios:00}   Reintegros: {_premiosService.ContadorReintegros:00}";

    public EscanearBoletosViewModel(
        PremiosReintegrosService premiosService,
        ConsultaPremiosService consultaService)
    {
        _premiosService = premiosService;
        _consultaService = consultaService;
        Title = "Escanear Boletos";
    }

    /// <summary>Permisos al APARECER (antes de crear el control de camara).</summary>
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
                Aviso = "Se necesita permiso de camara para escanear. Puedes capturar el codigo a mano.";
            }
        }
        catch (Exception)
        {
            CamaraLista = false;
            PermisoDenegado = true;
        }

        OnPropertyChanged(nameof(AcumuladoText));
    }

    /// <summary>Cadena cruda (camara o manual): parsea, dedupe, dialogo de resultado.</summary>
    public async Task ProcesarCadenaAsync(string cadena)
    {
        if (DialogoVisible)
        {
            return; // ya hay un boleto en pantalla esperando ACEPTAR
        }

        BilleteParseado? parseado = BilleteParser.ParsearCadena(cadena);
        if (parseado is null)
        {
            Aviso = "Codigo no reconocido. Verifica el boleto e intenta de nuevo.";
            return;
        }

        if (_premiosService.YaCapturado(parseado.CodigoCompleto))
        {
            Aviso = "Ese boleto ya fue capturado en este movimiento.";
            return;
        }

        Aviso = string.Empty;
        CodigoManual = string.Empty;

        (ResultadoPremio resultado, string detalle) = _consultaService.Consultar(parseado);

        SorteoInfo? sorteo = SorteoIdentificador.Identificar(parseado);
        string nombreSorteo = sorteo?.Nombre ?? "Loteria Nacional";
        if (sorteo?.NumeroSorteo is string num && num != "NA")
        {
            nombreSorteo = $"{nombreSorteo} {num}";
        }

        _boletoPendiente = new BoletoCapturado
        {
            Tipo = resultado == ResultadoPremio.TienePremio
                ? TipoCapturaBoleto.Premio
                : TipoCapturaBoleto.Reintegro,
            CodigoCompleto = parseado.CodigoCompleto,
            NombreSorteo = nombreSorteo,
            NumeroBillete = parseado.NumeroBillete ?? "-",
            SignoOSerie = parseado.EsZodiaco
                ? (parseado.SignoNombre ?? "-")
                : (parseado.Serie ?? "-"),
            Vig = parseado.Fraccion is int fr ? $"{fr:00}" : "-",
            Valor = resultado == ResultadoPremio.Reintegro
                ? PremiosReintegrosService.ValorReintegroEjemplo
                : 0m,
        };

        (MensajeDialogo, ColorAceptar) = resultado switch
        {
            ResultadoPremio.TienePremio => ("EL CACHITO TIENE PREMIO", "#4CB050"),
            ResultadoPremio.Reintegro => ("EL CACHITO TIENE REINTEGRO", "#FEC400"),
            _ => ("EL CACHITO NO TIENE PREMIO", "#FF5353"),
        };

        _dialogoAbiertoEnUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        DialogoVisible = true;
    }

    /// <summary>Recibe el texto decodificado por la camara (comando del control).</summary>
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

        // Leer y limpiar SIEMPRE (valido o invalido): evita concatenaciones.
        string valor = CodigoManual.Trim();
        CodigoManual = string.Empty;
        return ProcesarCadenaAsync(valor);
    }

    /// <summary>
    /// ACEPTAR del dialogo: agrega el boleto al acumulado y REGRESA AL
    /// ESCANER (escaneo continuo, regla del usuario).
    /// </summary>
    [RelayCommand]
    private void Aceptar()
    {
        if (_boletoPendiente is not null)
        {
            _premiosService.AgregarBoleto(_boletoPendiente);
        }

        _boletoPendiente = null;
        DialogoVisible = false;
        OnPropertyChanged(nameof(AcumuladoText));
    }

    /// <summary>Cierra el dialogo tocando el scrim (salida alternativa).</summary>
    [RelayCommand]
    private void CerrarDialogo()
    {
        // Tap fantasma: ignorar el toque que atraveso la transicion al abrir.
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _dialogoAbiertoEnUtcMs < 600)
        {
            return;
        }

        _boletoPendiente = null;
        DialogoVisible = false;
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

    /// <summary>Linterna on/off.</summary>
    [RelayCommand]
    private void ToggleLinterna()
    {
        LinternaEncendida = !LinternaEncendida;
    }
}
