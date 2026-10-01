using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Data;
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
/// Solo UI + estado local (sin backend): Confirmar Venta limpia el
/// carrito (las plecas verdes del catálogo compartido) y navega a la
/// pantalla de Venta Exitosa.
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
    /// Botón "Confirmar Venta": REGISTRA la venta en el backend
    /// (POST api/mobile/ventas/crear) con los id_billete reales del carrito,
    /// y solo si el backend confirma, vacía el carrito y abre la pantalla de
    /// Venta Exitosa.
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
            // La venta se registra primero. Si el backend la rechaza (409
            // boleto ya vendido, 403 ajeno, 422 sin precio) NO se navega:
            // el billetero tiene que saber que no se vendió.
            VentaCreadaApi venta = await _ventas.CrearVentaTecAsync(ids);

            FolioVenta = venta.Folio;

            // Solo ahora el carrito se vacía y se borra de SecureStorage:
            // la venta ya existe.
            await SorteosTecViewModel.Carrito.VaciarAsync();

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(nameof(Views.VentaExitosaTecPage));
            }
        }
        catch (ApiException ex)
        {
            // 409/403/422 del backend: mensaje real, sin limpiar el carrito
            // para que el billetero pueda reintentar o corregir.
            Error = ex.Message;
        }
        catch (Exception)
        {
            Error = "No se pudo registrar la venta. Revisa tu conexión e intenta de nuevo.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Folio real devuelto por el backend tras confirmar la venta.</summary>
    public string? FolioVenta { get; private set; }

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
