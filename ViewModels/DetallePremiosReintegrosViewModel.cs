using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel del detalle de un movimiento (mockups 3.1/3.2): header con
/// folio y fecha, resumen 3 columnas, pestanas Premios / Reintegros
/// con sorteos expandibles y tabla Billete | Signo | Vig. | Valor.
/// </summary>
[QueryProperty(nameof(FolioQuery), "folio")]
public partial class DetallePremiosReintegrosViewModel : BaseViewModel
{
    private readonly PremiosReintegrosService _servicio;

    private bool _pestanaPremios;

    public DetallePremiosReintegrosViewModel(PremiosReintegrosService servicio)
    {
        _servicio = servicio;
        Title = "Premios y Reintegros";
    }

    /// <summary>Folio recibido por navegacion (string del query param).</summary>
    public string FolioQuery { get; set; } = string.Empty;

    public MovimientoPremiosReintegros? Movimiento { get; private set; }

    [ObservableProperty]
    private string tituloHeader = string.Empty;

    [ObservableProperty]
    private string fecha = string.Empty;

    // Resumen (3 columnas del mockup)
    [ObservableProperty]
    private string premiosText = "$0.00";

    [ObservableProperty]
    private string reintegrosText = "$0.00";

    [ObservableProperty]
    private string totalText = "$0.00";

    /// <summary>True = pestana Premios visible; False = Reintegros (default).</summary>
    public bool PestanaPremios
    {
        get => _pestanaPremios;
        set
        {
            if (SetProperty(ref _pestanaPremios, value))
            {
                OnPropertyChanged(nameof(Sorteos));
                OnPropertyChanged(nameof(PestanaPremiosVisible));
                OnPropertyChanged(nameof(PestanaReintegrosVisible));
                OnPropertyChanged(nameof(ColorFondoPestanaPremios));
                OnPropertyChanged(nameof(ColorTextoPestanaPremios));
                OnPropertyChanged(nameof(ColorFondoPestanaReintegros));
                OnPropertyChanged(nameof(ColorTextoPestanaReintegros));
            }
        }
    }

    /// <summary>Sorteos de la pestana activa.</summary>
    public IEnumerable<SorteoPremiosReintegros> Sorteos =>
        PestanaPremios ? Movimiento?.SorteosPremios ?? Enumerable.Empty<SorteoPremiosReintegros>()
                       : Movimiento?.SorteosReintegros ?? Enumerable.Empty<SorteoPremiosReintegros>();

    public bool PestanaPremiosVisible => PestanaPremios;

    public bool PestanaReintegrosVisible => !PestanaPremios;

    // Colores de pestanas (patron ColorHex del proyecto: hex CON #)
    public string ColorFondoPestanaPremios => PestanaPremios ? "#4125F4" : "#E8E8F5";

    public string ColorTextoPestanaPremios => PestanaPremios ? "#FFFFFF" : "#4125F4";

    public string ColorFondoPestanaReintegros => PestanaPremios ? "#E8E8F5" : "#4125F4";

    public string ColorTextoPestanaReintegros => PestanaPremios ? "#4125F4" : "#FFFFFF";

    /// <summary>Se llama al aparecer la pagina (patron del proyecto).</summary>
    public void AlAparecer()
    {
        int folio = int.TryParse(FolioQuery, out int f) ? f : 0;
        Movimiento = _servicio.Movimientos.FirstOrDefault(m => m.Folio == folio);

        if (Movimiento is null)
        {
            return;
        }

        TituloHeader = $"Premios y Reintegros-{Movimiento.Folio}";
        Fecha = Movimiento.Fecha;
        PremiosText = $"${Movimiento.TotalPremios:0.00}";
        ReintegrosText = $"${Movimiento.TotalReintegros:0.00}";
        TotalText = $"${Movimiento.Total:0.00}";
        OnPropertyChanged(nameof(Sorteos));
    }

    /// <summary>Cambia de pestana (tap en el encabezado de Premios o Reintegros).</summary>
    [RelayCommand]
    private void CambiarPestana(string pestana)
    {
        PestanaPremios = pestana == "premios";
    }

    /// <summary>Tap en un sorteo: expande/contrae su tabla de boletos.</summary>
    [RelayCommand]
    private void ToggleSorteo(SorteoPremiosReintegros sorteo)
    {
        sorteo.Expandido = !sorteo.Expandido;
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
