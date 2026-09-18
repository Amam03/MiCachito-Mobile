using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services.Scanning;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla 2.1 (Consulta de premio despues de escaneo).
/// Recibe el codigo por QueryProperty, lo parsea, identifica el sorteo,
/// consulta el backend real (/api/mobile/premios/consultar) y expone los
/// campos para la tarjeta + desglose de montos.
///
/// Estados de resultado (colores del mockup 2.1):
///   GANADOR  -> verde #4CB050  + montos cachito/serie
///   REINTEGRO-> amarillo #FEC400 + monto del reintegro
///   NO GANADOR -> rojo #EE534F; el backend distingue VERIFICADO /
///     SIN_SABANA / SIN_SORTEO / AMBIGUO via 'motivo'.
/// </summary>
[QueryProperty(nameof(Codigo), "codigo")]
public partial class ResultadoConsultaPremiosViewModel : BaseViewModel
{
    private readonly ConsultaPremiosService _consultaService;

    public string Codigo { get; set; } = string.Empty;

    [ObservableProperty]
    private string nombreSorteo = string.Empty;

    [ObservableProperty]
    private string numeroBillete = string.Empty;

    [ObservableProperty]
    private string serieOsigno = string.Empty;

    [ObservableProperty]
    private string fraccion = string.Empty;

    [ObservableProperty]
    private string fecha = string.Empty;

    [ObservableProperty]
    private string mensajeResultado = string.Empty;

    /// <summary>Color del acento/boton segun resultado (hex con #, patron ColorHex del proyecto).</summary>
    [ObservableProperty]
    private string colorResultado = "#EE534F";

    /// <summary>True mientras dura la consulta al backend (overlay "Consultando...").</summary>
    [ObservableProperty]
    private bool consultando;

    /// <summary>Desglose de montos cuando hay premio o reintegro.</summary>
    [ObservableProperty]
    private string detalleMontos = string.Empty;

    /// <summary>Subtitulo del resultado (motivo del backend o aclaracion del reintegro).</summary>
    [ObservableProperty]
    private string subtituloResultado = string.Empty;

    /// <summary>Visibilidad de la fila de montos (patron DetallePago: bool plano para IsVisible).</summary>
    public bool HayMontos => !string.IsNullOrEmpty(DetalleMontos);

    /// <summary>Visibilidad del subtitulo de motivo.</summary>
    public bool HaySubtitulo => !string.IsNullOrEmpty(SubtituloResultado);

    public ResultadoConsultaPremiosViewModel(ConsultaPremiosService consultaService)
    {
        _consultaService = consultaService;
        Title = "Consulta de Premios";
    }

    /// <summary>Se llama al aparecer la pagina (patron del proyecto).</summary>
    public async Task AlAparecerAsync()
    {
        // Reset de estado por si la pagina se reutiliza (Shell push).
        Consultando = false;
        DetalleMontos = string.Empty;
        SubtituloResultado = string.Empty;

        BilleteParseado? parseado = BilleteParser.ParsearCadena(Uri.UnescapeDataString(Codigo));
        if (parseado is null)
        {
            MensajeResultado = "CODIGO INVALIDO";
            ColorResultado = "#EE534F";
            NombreSorteo = "No reconocido";
            return;
        }

        SorteoInfo? sorteo = SorteoIdentificador.Identificar(parseado);
        NombreSorteo = sorteo?.Nombre ?? "Loteria Nacional";
        if (sorteo?.NumeroSorteo is string num && num != "NA")
        {
            NombreSorteo = $"{NombreSorteo} {num}";
        }

        NumeroBillete = parseado.NumeroBillete ?? "-";
        SerieOsigno = parseado.EsZodiaco
            ? $"Signo {parseado.SignoNombre}"
            : $"Serie {parseado.Serie}";
        Fraccion = parseado.Fraccion is int f ? $"Fraccion {f}" : "-";
        Fecha = parseado.FechaSorteo ?? "-";

        await ConsultarBackendAsync(parseado);
    }

    private async Task ConsultarBackendAsync(BilleteParseado parseado)
    {
        Consultando = true;
        try
        {
            RespuestaPremioApi r = await _consultaService.ConsultarAsync(parseado);

            bool ganador = r.Resultado == "GANADOR";
            bool reintegro = r.Resultado == "REINTEGRO";

            (MensajeResultado, ColorResultado) = (ganador, reintegro) switch
            {
                (true, _) => ("EL CACHITO TIENE PREMIO", "#4CB050"),
                (_, true) => ("EL CACHITO TIENE REINTEGRO", "#FEC400"),
                _ => ("EL CACHITO NO TIENE PREMIO", "#EE534F"),
            };

            DetalleMontos = (ganador, reintegro) switch
            {
                (true, _) => $"Premio del cachito: {r.PremioCachito:C}" +
                             (r.PremioSerie > 0 ? $"  |  Billete completo: {r.PremioSerie:C}" : string.Empty) +
                             (r.TieneReintegro ? $"  |  Reintegro: {r.Reintegro:C}" : string.Empty),
                (_, true) => $"Reintegro: {r.Reintegro:C}",
                _ => string.Empty,
            };
            OnPropertyChanged(nameof(HayMontos));

            SubtituloResultado = r.Motivo switch
            {
                "SIN_SABANA" => "Resultados no disponibles para este sorteo",
                "SIN_SORTEO" => "La fecha del boleto no corresponde a un sorteo registrado",
                "AMBIGUO" => "Varios sorteos en la fecha: no se puede verificar",
                _ => string.Empty,
            };
            OnPropertyChanged(nameof(HaySubtitulo));
        }
        catch (TaskCanceledException)
        {
            // Timeout del HttpClient (30 s): el servidor no respondio.
            MensajeResultado = "SIN CONEXION AL SERVIDOR";
            ColorResultado = "#EE534F";
            SubtituloResultado = "El servidor no respondio. Verifica la conexion e intenta de nuevo";
            OnPropertyChanged(nameof(HaySubtitulo));
        }
        catch (OperationCanceledException)
        {
            MensajeResultado = "CONSULTA CANCELADA";
            ColorResultado = "#EE534F";
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            // Sin conexion / conexion rechazada: aviso SIN inventar un estado de premio.
            MensajeResultado = "SIN CONEXION AL SERVIDOR";
            ColorResultado = "#EE534F";
            SubtituloResultado = "Verifica la conexion e intenta de nuevo";
            OnPropertyChanged(nameof(HaySubtitulo));
        }
        catch (ApiException ex)
        {
            // HTTP/4xx del backend (sesion expirada, payload invalido).
            MensajeResultado = "NO SE PUDO COMPLETAR LA CONSULTA";
            ColorResultado = "#EE534F";
            SubtituloResultado = ex.Message;
            OnPropertyChanged(nameof(HaySubtitulo));
        }
        finally
        {
            Consultando = false;
        }
    }

    /// <summary>Aceptar regresa al escaner para consultar otro boleto.</summary>
    [RelayCommand]
    private Task AceptarAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }
        return Task.CompletedTask;
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
