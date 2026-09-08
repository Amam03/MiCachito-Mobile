using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Services.Scanning;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla 2.1 (Consulta de premio despues de escaneo).
/// Recibe el codigo por QueryProperty, lo parsea, identifica el sorteo,
/// consulta el resultado (mock) y expone los campos para la tarjeta.
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

    public ResultadoConsultaPremiosViewModel(ConsultaPremiosService consultaService)
    {
        _consultaService = consultaService;
        Title = "Consulta de Premios";
    }

    /// <summary>Se llama al aparecer la pagina (patron del proyecto).</summary>
    public void AlAparecer()
    {
        BilleteParseado? parseado = BilleteParser.ParsearCadena(Uri.UnescapeDataString(Codigo));
        if (parseado is null)
        {
            MensajeResultado = "CODIGO INVALIDO";
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

        (ResultadoPremio resultado, _) = _consultaService.Consultar(parseado);
        (MensajeResultado, ColorResultado) = resultado switch
        {
            ResultadoPremio.TienePremio => ("EL CACHITO TIENE PREMIO", "#4CB050"),
            ResultadoPremio.Reintegro => ("EL CACHITO TIENE REINTEGRO", "#FEC400"),
            _ => ("EL CACHITO NO TIENE PREMIO", "#EE534F"),
        };
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
