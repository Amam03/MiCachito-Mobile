using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// Selección de impresora Bluetooth (F6): lista los dispositivos
/// VINCULADOS reales (IImpresoraService.ObtenerDispositivosAsync),
/// permite seleccionar uno (se persiste la MAC en Preferences para
/// no volver a elegir en cada operación) y probar la conexión
/// (envía un ticket de prueba ESC/POS).
///
/// El emparejamiento NO se hace aquí: la impresora debe estar
/// VINCULADA desde Ajustes de Android (flujo del SO).
/// </summary>
public partial class ImpresoraViewModel : BaseViewModel
{
    private readonly IImpresoraService _impresora;

    public ObservableCollection<DispositivoBluetooth> Dispositivos { get; } = new();

    /// <summary>Dirección MAC persistida de la impresora seleccionada.</summary>
    [ObservableProperty]
    private string? impresoraGuardada;

    /// <summary>Texto del botón/estado de la impresora persistida.</summary>
    [ObservableProperty]
    private string estadoImpresora = "Sin impresora seleccionada";

    [ObservableProperty]
    private bool probando;

    /// <summary>Resultado de la impresión de prueba (visible con mensaje).</summary>
    [ObservableProperty]
    private string? mensajePrueba;

    public ImpresoraViewModel(IImpresoraService impresora)
    {
        _impresora = impresora;
    }

    /// <summary>Carga vinculados + seleccionada al aparecer.</summary>
    public async Task AlAparecerAsync()
    {
        await CargarDispositivosAsync();
    }

    [RelayCommand]
    private async Task CargarDispositivosAsync()
    {
        Dispositivos.Clear();
        var dispositivos = await _impresora.ObtenerDispositivosAsync();
        foreach (var d in dispositivos)
        {
            Dispositivos.Add(d);
        }

        string? mac = _impresora.ImpresoraSeleccionada();
        ImpresoraGuardada = mac;
        var elegida = dispositivos.FirstOrDefault(d => d.Direccion == mac);
        EstadoImpresora = elegida is not null
            ? $"Impresora seleccionada: {elegida.DisplayName}"
            : "Sin impresora seleccionada";

        MensajePrueba = null;
    }

    /// <summary>Selecciona (persiste la MAC) al tocar un dispositivo.</summary>
    [RelayCommand]
    private void SeleccionarDispositivo(DispositivoBluetooth d)
    {
        _impresora.SeleccionarImpresora(d.Direccion);
        ImpresoraGuardada = d.Direccion;
        EstadoImpresora = $"Impresora seleccionada: {d.DisplayName}";
        MensajePrueba = null;
    }

    /// <summary>
    /// Prueba la conexión: envía un ticket de prueba ESC/POS a la
    /// seleccionada (sin datos de negocio).
    /// </summary>
    [RelayCommand]
    private async Task ProbarAsync()
    {
        if (Probando)
        {
            return;
        }

        Probando = true;
        MensajePrueba = null;
        try
        {
            var prueba = new ComprobanteLotenal
            {
                Tipo = "PRUEBA DE IMPRESION",
                Folio = "PRUEBA",
                FechaHora = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                Billetero = "MIA CACHITO",
                ProductoSorteo = "Conexion correcta",
                Lineas = ["Ticket de prueba", "Si lees esto la impresora funciona"],
                Total = "$0.00",
            };
            ResultadoImpresion r = await _impresora.ImprimirComprobanteAsync(prueba);
            MensajePrueba = r.Mensaje;
        }
        finally
        {
            Probando = false;
        }
    }

    /// <summary>Regresa a la pantalla anterior (patrón del proyecto).</summary>
    [RelayCommand]
    private async Task VolverAsync()
    {
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
