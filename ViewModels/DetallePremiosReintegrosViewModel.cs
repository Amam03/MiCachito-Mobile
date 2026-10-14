using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel del detalle de un movimiento (mockups 3.1/3.2): header con
/// folio y fecha, resumen 3 columnas, pestañas Premios / Reintegros con
/// sorteos expandibles y tabla Billete | Signo | Vig. | Valor.
///
/// Conectado al backend (2026-10-13): carga el detalle AGRUPADO POR
/// SORTEO (D2) con GET api/mobile/premios-reintegros/{id} en
/// AlAparecerAsync; el folio del header es el del servidor (PYR-..., D3).
/// Movimiento ajeno → 404: se muestra el aviso, sin datos.
/// </summary>
[QueryProperty(nameof(IdQuery), "id")]
public partial class DetallePremiosReintegrosViewModel : BaseViewModel
{
    private readonly PremiosReintegrosService _servicio;

    private bool _pestanaPremios;

    public DetallePremiosReintegrosViewModel(PremiosReintegrosService servicio)
    {
        _servicio = servicio;
        Title = "Premios y Reintegros";
    }

    /// <summary>Id de movimiento recibido por navegación (string del query param).</summary>
    public string? IdQuery { get; set; }

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

    /// <summary>True mientras corre el GET del detalle (overlay).</summary>
    [ObservableProperty]
    private bool cargando;

    /// <summary>Mensaje de error de la carga (404 ajeno / sin conexión).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    public bool HayError => !string.IsNullOrEmpty(MensajeError);

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

    /// <summary>Carga el detalle real (D2) al aparecer la página.</summary>
    public async Task AlAparecerAsync()
    {
        if (Cargando)
        {
            return;
        }

        Cargando = true;
        MensajeError = string.Empty;
        try
        {
            try
            {
                if (!int.TryParse(IdQuery, out int id) || id <= 0)
                {
                    MensajeError = "Movimiento no válido";
                    return;
                }

                DetallePyRApi detalle = await _servicio.CargarDetalleAsync(id);
                var movimiento = new MovimientoPremiosReintegros
                {
                    IdMovimiento = detalle.IdMovimiento,
                    FolioTexto = detalle.Folio,
                    Fecha = FechaFormatoMockup(detalle.FechaMovimiento),
                    TotalPremiosApi = detalle.TotalPremios,
                    TotalReintegrosApi = detalle.TotalReintegros,
                };
                movimiento.CargarDetalle(detalle);
                Movimiento = movimiento;

                TituloHeader = $"Premios y Reintegros-{detalle.Folio}";
                Fecha = movimiento.Fecha;
                PremiosText = $"${detalle.TotalPremios:0.00}";
                ReintegrosText = $"${detalle.TotalReintegros:0.00}";
                TotalText = $"${detalle.TotalGeneral:0.00}";
                OnPropertyChanged(nameof(Sorteos));
            }
            catch (TaskCanceledException)
            {
                MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            }
            catch (OperationCanceledException)
            {
                MensajeError = "Consulta cancelada";
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            }
            catch (ApiException ex)
            {
                MensajeError = $"No se pudo cargar el movimiento: {ex.Message}";
            }
        }
        finally
        {
            Cargando = false;
            OnPropertyChanged(nameof(HayError));
        }
    }

    /// <summary>"yyyy-MM-dd HH:mm:ss" del backend → "DD-mes-AAAA" del mockup.</summary>
    private static string FechaFormatoMockup(string? cruda)
    {
        if (Helpers.FormatosFecha.TryParseFechaBackend(cruda, out DateOnly fecha))
        {
            return Helpers.FormatosFecha.FechaLarga(fecha);
        }
        return cruda ?? string.Empty;
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
