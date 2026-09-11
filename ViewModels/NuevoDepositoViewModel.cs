using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Platforms.Android.Services;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de "Nuevo Depósito" (mockups 7.1/7.2 de Gestión): formulario
/// con selectores de banco/fecha/hora, folio y monto (dial-pad estilo
/// teléfono de la pantalla 10), observaciones (nombre del sorteo),
/// comprobante adjunto y validaciones rojas al Aceptar. Fase
/// solo-interfaz: el registro se guarda en memoria (DepositoService).
/// </summary>
public partial class NuevoDepositoViewModel : BaseViewModel
{
    private readonly DepositoService _depositos;

    /// <summary>Bancos del selector (catálogo del mockup 7.2).</summary>
    public IReadOnlyList<string> Bancos => DepositoService.Bancos;

    // ---------- Estado del formulario ----------

    [ObservableProperty]
    private string bancoSeleccionado = string.Empty;

    [ObservableProperty]
    private string fechaTexto = string.Empty;

    [ObservableProperty]
    private string horaTexto = string.Empty;

    [ObservableProperty]
    private string folioMovimiento = string.Empty;

    [ObservableProperty]
    private string montoTexto = string.Empty;

    [ObservableProperty]
    private string observaciones = string.Empty;

    [ObservableProperty]
    private string nombreComprobante = string.Empty;

    /// <summary>Fecha elegida (null hasta seleccionar).</summary>
    public DateTime? FechaDeposito { get; private set; }

    /// <summary>Hora elegida (null hasta seleccionar).</summary>
    public TimeSpan? HoraDeposito { get; private set; }

    /// <summary>Monto tecleado en el dial-pad, ya con decimal.</summary>
    public decimal MontoValor { get; private set; }

    // ---------- Estado del selector de banco (overlay) ----------

    [ObservableProperty]
    private bool selectorBancoVisible;

    /// <summary>Ignora el toque del scrim ~600 ms tras abrir (tap fantasma).</summary>
    private DateTime _abiertoEnUtc = DateTime.MinValue;

    // ---------- Estado del dial-pad del monto ----------

    [ObservableProperty]
    private bool tecladoMontoVisible;

    /// <summary>Notifica el padding derivado del dial-pad.</summary>
    partial void OnTecladoMontoVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(PaddingPad));
    }

    /// <summary>Cadena cruda del dial-pad (ej. "1", "1.", "1.5", "1.50").</summary>
    [ObservableProperty]
    private string montoCaptura = string.Empty;

    // ---------- Validaciones (avisos rojos del mockup 7.2) ----------

    [ObservableProperty]
    private bool avisoBanco;

    [ObservableProperty]
    private bool avisoFecha;

    [ObservableProperty]
    private bool avisoHora;

    [ObservableProperty]
    private bool avisoFolio;

    [ObservableProperty]
    private bool avisoMonto;

    [ObservableProperty]
    private bool avisoObservaciones;

    /// <summary>True tras el primer Aceptar: los campos se revalidan al cambiar.</summary>
    private bool _validacionActiva;

    // ---------- Contadores de caracteres (0/20, 0/50) ----------

    public string FolioContador => $"{FolioMovimiento.Length}/20";
    public string ObservacionesContador => $"{Observaciones.Length}/50";
    public string MontoContador => $"{MontoCaptura.Length}/20";

    // ---------- Derivadas de visibilidad (valor vs hint) ----------

    public bool TieneBanco => !string.IsNullOrWhiteSpace(BancoSeleccionado);
    public bool SinBanco => !TieneBanco;
    public bool TieneFecha => FechaDeposito is not null;
    public bool SinFecha => !TieneFecha;
    public bool TieneHora => HoraDeposito is not null;
    public bool SinHora => !TieneHora;
    public bool TieneMonto => !string.IsNullOrEmpty(MontoTexto);
    public bool SinMonto => !TieneMonto;
    public bool TieneComprobante => !string.IsNullOrWhiteSpace(NombreComprobante);
    public bool SinComprobante => !TieneComprobante;

    /// <summary>Padding del contenido: márgenes del mockup 7.1 + 300 extra abajo cuando el pad está abierto (sustituye al BoxView espaciador).</summary>
    public Thickness PaddingPad => TecladoMontoVisible
        ? new Thickness(20, 14, 20, 310)
        : new Thickness(20, 14, 20, 10);

    public NuevoDepositoViewModel(DepositoService depositos)
    {
        _depositos = depositos;
        Title = "Nuevo Depósito";
    }

    partial void OnFolioMovimientoChanged(string value) => NotificarEstado();
    partial void OnObservacionesChanged(string value) => NotificarEstado();

    /// <summary>Refresca contadores, avisos y derivadas de visibilidad.</summary>
    private void NotificarEstado()
    {
        OnPropertyChanged(nameof(FolioContador));
        OnPropertyChanged(nameof(ObservacionesContador));
        OnPropertyChanged(nameof(MontoContador));
        OnPropertyChanged(nameof(TieneBanco));
        OnPropertyChanged(nameof(SinBanco));
        OnPropertyChanged(nameof(TieneFecha));
        OnPropertyChanged(nameof(SinFecha));
        OnPropertyChanged(nameof(TieneHora));
        OnPropertyChanged(nameof(SinHora));
        OnPropertyChanged(nameof(TieneMonto));
        OnPropertyChanged(nameof(SinMonto));
        OnPropertyChanged(nameof(TieneComprobante));
        OnPropertyChanged(nameof(SinComprobante));

        if (!_validacionActiva)
        {
            return;
        }

        AvisoBanco = string.IsNullOrWhiteSpace(BancoSeleccionado);
        AvisoFecha = FechaDeposito is null;
        AvisoHora = HoraDeposito is null;
        AvisoFolio = string.IsNullOrWhiteSpace(FolioMovimiento);
        AvisoMonto = MontoValor <= 0;
        AvisoObservaciones = string.IsNullOrWhiteSpace(Observaciones);
    }

    // ---------- Selector de banco (overlay) ----------

    [RelayCommand]
    private void AbrirSelectorBanco()
    {
        SelectorBancoVisible = true;
        _abiertoEnUtc = DateTime.UtcNow;
    }

    [RelayCommand]
    private void CerrarSelectorBanco()
    {
        if ((DateTime.UtcNow - _abiertoEnUtc).TotalMilliseconds < 600)
        {
            return; // tap fantasma del apertura
        }
        SelectorBancoVisible = false;
    }

    [RelayCommand]
    private void SeleccionarBanco(string banco)
    {
        BancoSeleccionado = banco;
        SelectorBancoVisible = false;
        NotificarEstado();
    }

    // ---------- Fecha (MaterialDatePicker calendario) ----------

    [RelayCommand]
    private async Task ElegirFechaAsync()
    {
        DateTime? elegida = await DialogoFechaHoraService.PickFechaAsync(FechaDeposito ?? DateTime.Today);
        if (elegida is not null)
        {
            FechaDeposito = elegida;
            FechaTexto = elegida.Value.ToString("dd/MM/yyyy");
            NotificarEstado();
        }
    }

    // ---------- Hora (MaterialTimePicker teclado) ----------

    [RelayCommand]
    private async Task ElegirHoraAsync()
    {
        TimeSpan? elegida = await DialogoFechaHoraService.PickHoraAsync(HoraDeposito ?? new TimeSpan(13, 43, 0));
        if (elegida is not null)
        {
            HoraDeposito = elegida;
            HoraTexto = $"{elegida:hh\\:mm}";
            NotificarEstado();
        }
    }

    // ---------- Dial-pad del monto (estilo pantalla 10, 4 columnas) ----------

    [RelayCommand]
    private void AbrirTecladoMonto()
    {
        TecladoMontoVisible = true;
    }

    [RelayCommand]
    private void TeclaMonto(string digito)
    {
        if (digito == ".")
        {
            if (!MontoCaptura.Contains('.'))
            {
                MontoCaptura = string.IsNullOrEmpty(MontoCaptura) ? "0." : MontoCaptura + ".";
            }
        }
        else
        {
            if (MontoCaptura.Contains('.'))
            {
                string decimales = MontoCaptura.Split('.')[1];
                if (decimales.Length >= 2)
                {
                    return;
                }
            }
            if (MontoCaptura.Length >= 20)
            {
                return;
            }
            MontoCaptura += digito;
        }

        OnPropertyChanged(nameof(MontoCaptura));
    }

    [RelayCommand]
    private void BorrarDigitoMonto()
    {
        if (MontoCaptura.Length > 0)
        {
            MontoCaptura = MontoCaptura[..^1];
            OnPropertyChanged(nameof(MontoCaptura));
        }
    }

    /// <summary>"Realiz." cierra el pad y fija el monto (patrón pantalla 10).</summary>
    [RelayCommand]
    private void RealizadoMonto()
    {
        if (decimal.TryParse(MontoCaptura, System.Globalization.CultureInfo.InvariantCulture,
                out decimal valor))
        {
            MontoValor = valor;
            MontoTexto = $"${valor:0.00}";
        }
        else
        {
            MontoValor = 0;
            MontoTexto = string.Empty;
        }

        TecladoMontoVisible = false;
        NotificarEstado();
    }

    /// <summary>✕ del pad: limpia la captura y el monto fijado.</summary>
    [RelayCommand]
    private void LimpiarMonto()
    {
        MontoCaptura = string.Empty;
        MontoValor = 0;
        MontoTexto = string.Empty;
        OnPropertyChanged(nameof(MontoCaptura));
    }

    // ---------- Comprobante (selector nativo de archivos) ----------

    [RelayCommand]
    private async Task AdjuntarComprobanteAsync()
    {
        try
        {
            var tipos = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "image/*" } },
            });

            FileResult? archivo = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona el comprobante de depósito",
                FileTypes = tipos,
            });

            if (archivo is not null)
            {
                NombreComprobante = archivo.FileName;
            }
        }
        catch (TaskCanceledException)
        {
            // el usuario canceló el selector
        }
    }

    // ---------- Validación y guardado ----------

    /// <summary>Aceptar: valida; si falta algo muestra los avisos rojos
    /// (mockup 7.2) y NO guarda. Completo: guarda y regresa al listado.</summary>
    [RelayCommand]
    private async Task AceptarAsync()
    {
        _validacionActiva = true;
        NotificarEstado();

        if (AvisoBanco || AvisoFecha || AvisoHora || AvisoFolio
            || AvisoMonto || AvisoObservaciones)
        {
            return;
        }

        _depositos.Guardar(new Deposito
        {
            Banco = BancoSeleccionado,
            FechaDeposito = FechaDeposito!.Value,
            HoraDeposito = HoraDeposito!.Value,
            FolioMovimiento = FolioMovimiento.Trim(),
            Monto = MontoValor,
            Observaciones = Observaciones.Trim(),
            NombreComprobante = string.IsNullOrWhiteSpace(NombreComprobante) ? null : NombreComprobante,
        });

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    /// <summary>Cancelar: regresa al listado sin guardar.</summary>
    [RelayCommand]
    private Task CancelarAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }
        return Task.CompletedTask;
    }
}
