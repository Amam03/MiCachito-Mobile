using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Requests;
using MiCachito.Mobile.Models.Responses;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Datos del Cliente" (pantalla 15), a la que
/// se llega desde Vender del carrito Tec (pantalla 14).
///
/// Formulario (mockup 15): Nombre, Apellido Paterno, Apellido Materno,
/// Teléfono, Correo Electrónico y Seleccionar Estado (picker con las 32
/// entidades federativas, catálogo EstadosData por decisión del usuario).
///
/// Confirmar Venta REGISTRA la venta en el backend (POST
/// api/mobile/ventas/crear) con los id_billete reales del carrito; solo si
/// el backend la acepta se limpia el carrito y se abre Venta Exitosa, con
/// el folio y el total REALES devueltos por el servidor.
/// </summary>
public partial class DatosClienteTecViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _apellidoPaterno = string.Empty;

    [ObservableProperty]
    private string _apellidoMaterno = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _correoElectronico = string.Empty;

    [ObservableProperty]
    private string? _estadoSeleccionado;

    public DatosClienteTecViewModel()
    {
        Title = "Datos del Cliente";
    }

    /// <summary>Estados del picker: 32 entidades federativas.</summary>
    private readonly MobileVentasService _ventas;

    /// <summary>
    /// Inyectado por constructor (mismo patrón que SorteosViewModel).
    /// </summary>
    public DatosClienteTecViewModel(MobileVentasService ventas)
    {
        Title = "Datos del Cliente";
        _ventas = ventas;
    }

    /// <summary>Estados del picker: 32 entidades federativas.</summary>
    public ObservableCollection<string> Estados { get; } = new(EstadosData.ObtenerEstados());

    /// <summary>
    /// Ultimo error de la venta, para mostrarlo en la pantalla en vez de
    /// navegar como si todo hubiera ido bien.
    /// </summary>
    [ObservableProperty]
    private string? _error;

    /// <summary>
    /// Datos del cliente tal como los escribio el billetero, listos para la
    /// peticion de venta.
    ///
    /// Devuelve null si no hay nombre: en ese caso el backend registra la
    /// venta sin cliente en vez de crear un registro vacio en `clientes`.
    /// Preferimos una columna Cliente vacia a un cliente sin nombre.
    /// </summary>
    private ClienteVenta? cliente
    {
        get
        {
            var datos = new ClienteVenta
            {
                Nombre = (Nombre ?? string.Empty).Trim(),
                ApellidoPaterno = (ApellidoPaterno ?? string.Empty).Trim(),
                ApellidoMaterno = (ApellidoMaterno ?? string.Empty).Trim(),
                Telefono = (Telefono ?? string.Empty).Trim(),
                Correo = (CorreoElectronico ?? string.Empty).Trim(),
            };

            return datos.EsUtilizable ? datos : null;
        }
    }

    /// <summary>
    /// Botón "Confirmar Venta": REGISTRA la venta en el backend
    /// (POST api/mobile/ventas/crear) con los id_billete reales del carrito.
    ///
    /// Está partida en DOS FASES para que un fallo posterior a la venta ya
    /// registrada NUNCA se presente como una venta fallida (que llevaría al
    /// billetero a reintentar y recibir un 409 "boleto ya vendido"):
    ///
    ///  - FASE A: solo el POST. Aquí la venta todavía NO existe, así que un
    ///    409/403/422 o un corte de red SÍ son un fallo de venta: se muestra
    ///    el mensaje, NO se navega y el carrito queda intacto.
    ///  - FASE B: la venta YA existe en el servidor. Se conservan folio y
    ///    total del backend; limpiar el carrito o navegar ya no pueden
    ///    reportarse como venta fallida.
    ///
    /// ANTES: solo limpiaba el carrito y navegaba, sin llamar a la API. El
    /// usuario veía un comprobante de una venta que NUNCA se había
    /// registrado: ni boleto marcado como vendido, ni folio, ni nada que
    /// aparezca en el reporte de escritorio.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmarVentaAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IReadOnlyList<int> ids = SorteosTecViewModel.Carrito.IdsBilletes();
        if (ids.Count == 0)
        {
            Error = "Agrega al menos un boleto antes de confirmar la venta.";
            return;
        }

        IsBusy = true;
        Error = null;

        try
        {
            // ── FASE A: único punto reportable como venta fallida ─────────
            // El backend valida pertenencia, estado 'asignado' y precio. Si
            // lo rechaza (409 boleto ya vendido, 403 ajeno, 422 sin precio)
            // NO se navega NI se vacía el carrito: el billetero tiene que
            // saber que no se vendió.
            //
            // Los datos del cliente viajan en la MISMA petición (antes se
            // descartaban aqui y la venta quedaba sin id_cliente, por lo que
            // la columna Cliente del reporte de escritorio salia vacia).
            VentaCreadaApi venta;

            try
            {
                venta = await _ventas.CrearVentaTecAsync(ids, cliente);
            }
            catch (ApiException ex)
            {
                Error = ex.Message;
                return;
            }
            catch (Exception)
            {
                Error = "No se pudo registrar la venta. Revisa tu conexión e intenta de nuevo.";
                return;
            }

            // ── FASE B: la venta YA existe en el servidor ─────────────────
            // Folio y total reales del backend; Mobile no recalcula nada.
            FolioVenta = venta.Folio;
            TotalVentaTexto = $"${venta.Total:0.00}";

            try
            {
                // Best-effort: la venta ya está registrada aunque el carrito
                // local no se pudiera limpiar (se borra de SecureStorage).
                await SorteosTecViewModel.Carrito.VaciarAsync();
            }
            catch (Exception)
            {
            }

            if (Shell.Current is null)
            {
                await MostrarVentaRegistradaAsync();
                return;
            }

            try
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(Views.VentaExitosaTecPage)}" +
                    $"?folio={Uri.EscapeDataString(FolioVenta)}" +
                    $"&total={Uri.EscapeDataString(TotalVentaTexto)}");
            }
            catch (Exception)
            {
                // La navegación falló, pero la venta existe. No es un fallo
                // de venta: se consulta el resultado aquí mismo y la flecha
                // ← (GoBackCommand) lleva al carrito, ya vacío. El POST
                // NUNCA se vuelve a enviar.
                await MostrarVentaRegistradaAsync();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Folio real devuelto por el backend tras confirmar la venta.</summary>
    [ObservableProperty]
    private string folioVenta = string.Empty;

    /// <summary>
    /// Total real formateado ("$140.00"), tal como lo devuelve el backend.
    /// </summary>
    [ObservableProperty]
    private string totalVentaTexto = string.Empty;

    /// <summary>
    /// Aviso cuando la venta ya quedó registrada pero no se pudo navegar a
    /// Venta Exitosa (Shell caído). Se muestran el folio y el total REALES
    /// del backend para que el billetero consulte el resultado sin volver a
    /// confirmar la venta. Mismo patrón de diálogo que usa
    /// DetalleVentaViewModel y ExpendiosViewModel.
    /// </summary>
    private async Task MostrarVentaRegistradaAsync()
    {
        if (Application.Current?.Windows?.FirstOrDefault()?.Page is not { } pagina)
        {
            return;
        }

        await pagina.DisplayAlertAsync(
            "Venta registrada",
            $"La venta ya quedó registrada en el servidor.\n\n" +
            $"Folio: {FolioVenta}\n" +
            $"Total: {TotalVentaTexto}\n\n" +
            "El carrito quedó vacío. Usa la flecha ← para continuar; " +
            "NO vuelvas a confirmar esta venta.",
            "Aceptar");
    }

    /// <summary>
    /// Retrocede a la pantalla anterior (carrito Tec).
    /// </summary>
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
