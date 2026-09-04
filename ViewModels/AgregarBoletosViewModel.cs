using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Agregar Boletos" (pantallas 9.1 / 9.2).
/// Aparece tras seleccionar la ciudad (pantalla 8) en el flujo de venta
/// LOTENAL y muestra las tiendas/boletos disponibles.
///
/// Recibe vía QueryProperty (navegación Shell):
///   - sorteoId: sorteo activo seleccionado (pantalla 7.x)
///   - tipoSorteoId: tipo de sorteo (pantalla 6)
///   - ciudadId: ciudad seleccionada (pantalla 8); 0 = "Cualquier ciudad"
///
/// REGLA DE NEGOCIO (ciudad): ciudadId = 0 muestra todas las filas;
/// una ciudad específica muestra solo sus filas.
///
/// VARIACIÓN ZODIACO (tipos 3 = Zodiaco y 4 = Zodiaco Especial):
///   - Cada fila añade la línea de SIGNO.
///   - Encima de la lista hay una fila filtro "Signo Aleatorio ▼";
///     al tocarla se despliega el selector de 13 opciones
///     ("Signo Aleatorio" + 12 signos).
///   - "Signo Aleatorio" = sin filtro (mismo patrón que
///     "Cualquier ciudad"); un signo específico filtra la lista.
///
/// PANTALLA 10.x (Cantidad de Cachitos): al tocar el carrito de una fila
/// se abre un overlay con diálogo + teclado numérico sobre esta misma
/// pantalla (la lista queda atenuada detrás, como en los mockups):
///   - Teclado formato teléfono en 4 filas: 1-2-3 / 4-5-6 / 7-8-9 /
///     Borrar-0-Realizado. El campo inicia VACÍO y las teclas agregan
///     dígitos (máx. 2); Borrar elimina el último.
///   - "Realizado" OCULTA el teclado (queda el diálogo junto a la fila)
///     y muestra el toast "AQUÍ ELEGIRÁS {n} DE {disp} / DISPONIBLES"
///     (mockup 10.2).
///   - "Aceptar" aplica la cantidad a la fila: el registro pasa de
///     "0/20" a "n/20" (Seleccionadas de TiendaDisponible) y cierra el
///     overlay. "Cancelar" (o tocar el scrim) cierra sin aplicar.
///   - Los "disponibles" del diálogo se DERIVAN de la fila seleccionada
///     (TotalDisponible de TiendaDisponible): no hay datos hardcoded.
///
/// PANTALLA 11 (Carrito de Compras): el carrito del header navega a
/// CarritoComprasPage con sorteoId/tipoSorteoId. El badge del header
/// cuenta las tiendas con selección (registros del carrito): al volver
/// de aquella pantalla (OnAppearing → AlAparecer) se refresca, porque
/// Eliminar/Vender pudieron cambiar las selecciones.
/// </summary>
[QueryProperty(nameof(SorteoIdStr), "sorteoId")]
[QueryProperty(nameof(TipoSorteoIdStr), "tipoSorteoId")]
[QueryProperty(nameof(CiudadIdStr), "ciudadId")]
public partial class AgregarBoletosViewModel : BaseViewModel
{
    /// <summary>Ids de los tipos de sorteo zodiacales (ver SorteosLotenalData).</summary>
    private const int TipoZodiaco = 3;
    private const int TipoZodiacoEspecial = 4;

    [ObservableProperty]
    private ObservableCollection<TiendaDisponible> _tiendas = new();

    [ObservableProperty]
    private ObservableCollection<SignoZodiaco> _signos = new();

    /// <summary>
    /// True cuando el selector de signos está desplegado (solo zodiaco).
    /// </summary>
    [ObservableProperty]
    private bool _selectorSignoVisible;

    /// <summary>
    /// Texto de la fila filtro: "Signo Aleatorio" o el signo elegido.
    /// </summary>
    [ObservableProperty]
    private string _signoFiltroTexto = "Signo Aleatorio";

    /// <summary>
    /// True para Zodiaco (3) y Zodiaco Especial (4): activa la fila
    /// filtro de signo, la línea de signo por fila y el selector.
    /// Se notifica a la UI al recibir el tipo por navegación.
    /// </summary>
    [ObservableProperty]
    private bool _esZodiaco;

    // ============ Pantalla 10.x: Cantidad de Cachitos ============

    /// <summary>True cuando el overlay 10.x (diálogo + teclado) está abierto.</summary>
    [ObservableProperty]
    private bool _cantidadDialogoVisible;

    /// <summary>True cuando el teclado numérico está visible (Realizado lo oculta).</summary>
    [ObservableProperty]
    private bool _tecladoVisible;

    /// <summary>Fila cuyo carrito se tocó (tienda/boleto en contexto).</summary>
    [ObservableProperty]
    private TiendaDisponible? _tiendaSeleccionada;

    /// <summary>Cantidad capturada en el campo del diálogo (inicia vacío).</summary>
    [ObservableProperty]
    private string _cantidadTexto = string.Empty;

    /// <summary>True tras pulsar "Realizado": muestra el toast inferior.</summary>
    [ObservableProperty]
    private bool _toastCantidadVisible;

    /// <summary>
    /// Cantidad visible en el toast tras "Realizado" (mockup 10.2).
    /// Aceptar la usa si el campo quedó vacío al ocultar el teclado.
    /// </summary>
    [ObservableProperty]
    private int _cantidadConfirmada;

    /// <summary>Momento (UTC) en que se abrió el overlay 10.x.</summary>
    private DateTime _abiertoEnUtc = DateTime.MinValue;

    /// <summary>
    /// Ventana (ms) tras la carga inicial en la que se ignora la apertura
    /// del diálogo de cantidad: defensa anti "tap fantasma" (el tap que
    /// eligió la ciudad puede atravesar la transición y golpear el
    /// carrito de una fila). Mismo patrón que el selector de signos.
    /// </summary>
    private const int MsIgnorarTrasCarga = 600;

    /// <summary>
    /// Ventana (ms) tras ABRIR el overlay 10.x en la que se ignora el
    /// toque sobre el scrim: el tap del carrito que abrió el overlay
    /// puede atravesar la aparición del scrim y cerrarlo de inmediato
    /// (tap fantasma, mismo fenómeno que el selector de signos).
    /// </summary>
    private const int MsIgnorarCierreTrasAbrir = 600;

    private int? _sorteoId;
    private int? _tipoSorteoId;
    private int? _ciudadId;
    private bool _cargado;
    private DateTime _cargadoEnUtc = DateTime.MinValue;
    private SignoZodiaco _signoFiltro = SignosZodiacoData.SignoAleatorio;

    /// <summary>
    /// Disponibles de la fila seleccionada (mockups: "20"/"16" junto al
    /// campo). Derivado de la propia fila; vacío sin selección.
    /// </summary>
    public string DisponiblesTexto => TiendaSeleccionada is null
        ? string.Empty
        : TiendaSeleccionada.TotalDisponible.ToString();

    /// <summary>
    /// Línea 1 del toast (mockup 10.2): "AQUÍ ELEGIRÁS {n} DE {disp}",
    /// con n = cantidad confirmada y disp = disponibles de la fila.
    /// </summary>
    public string ToastLinea1 =>
        $"AQUÍ ELEGIRÁS {CantidadConfirmada} DE {DisponiblesTexto}";

    /// <summary>
    /// Tiendas con selección en el catálogo completo del tipo de sorteo
    /// (no solo la lista filtrada): coincide con los registros que
    /// muestra la pantalla del carrito (11).
    /// </summary>
    private int TiendasConSeleccion()
    {
        if (_tipoSorteoId is null)
        {
            return 0;
        }

        return TiendasDisponiblesData.ObtenerPorCiudad(0, EsZodiaco)
            .Count(t => t.Seleccionadas > 0);
    }

    /// <summary>
    /// Badge del carrito del header: TIENDAS con selección (registros
    /// del carrito), en dos dígitos ("00" sin registros, como en los
    /// mockups 10.x). Cada tienda distinta cuenta 1.
    /// </summary>
    public string CarritoBadgeTexto => TiendasConSeleccion().ToString("00");

    /// <summary>
    /// El badge se muestra con el overlay abierto (mockups 10.x) o
    /// cuando ya hay registros en el carrito.
    /// </summary>
    public bool MostrarBadgeCarrito =>
        CantidadDialogoVisible || TiendasConSeleccion() > 0;

    /// <summary>
    /// Id del sorteo activo, recibido como string vía navegación Shell.
    /// </summary>
    public string? SorteoIdStr
    {
        get => _sorteoId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _sorteoId = id;
                IntentarCargar();
            }
        }
    }

    /// <summary>
    /// Id del tipo de sorteo, recibido como string vía navegación Shell.
    /// Al asignarse se calcula EsZodiaco.
    /// </summary>
    public string? TipoSorteoIdStr
    {
        get => _tipoSorteoId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _tipoSorteoId = id;
                EsZodiaco = id is TipoZodiaco or TipoZodiacoEspecial;
                IntentarCargar();
            }
        }
    }

    /// <summary>
    /// Id de la ciudad seleccionada (0 = "Cualquier ciudad"),
    /// recibido como string vía navegación Shell.
    /// </summary>
    public string? CiudadIdStr
    {
        get => _ciudadId?.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                _ciudadId = id;
                IntentarCargar();
            }
        }
    }

    public AgregarBoletosViewModel()
    {
        Title = "Agregar Boletos";
    }

    /// <summary>
    /// Notifica a la UI las propiedades derivadas de la pantalla 10.x
    /// (llamar tras cualquier cambio de estado del overlay o de las
    /// selecciones de las filas).
    /// </summary>
    private void NotificarDerivadasCantidad()
    {
        OnPropertyChanged(nameof(DisponiblesTexto));
        OnPropertyChanged(nameof(ToastLinea1));
        OnPropertyChanged(nameof(CarritoBadgeTexto));
        OnPropertyChanged(nameof(MostrarBadgeCarrito));
    }

    /// <summary>
    /// Carga la lista solo cuando los tres parámetros de navegación
    /// están disponibles (Shell los asigna en orden indeterminado).
    /// </summary>
    private void IntentarCargar()
    {
        if (_cargado || _sorteoId is null || _tipoSorteoId is null || _ciudadId is null)
        {
            return;
        }

        _cargado = true;
        _cargadoEnUtc = DateTime.UtcNow;
        Signos = new ObservableCollection<SignoZodiaco>(SignosZodiacoData.ObtenerTodas());
        AplicarFiltros();
    }

    /// <summary>
    /// Aplica los filtros activos (ciudad + signo) sobre el catálogo
    /// correspondiente y refresca la lista.
    /// </summary>
    private void AplicarFiltros()
    {
        if (_ciudadId is null)
        {
            return;
        }

        var filas = TiendasDisponiblesData.ObtenerPorCiudad(_ciudadId.Value, EsZodiaco);

        if (EsZodiaco && !_signoFiltro.EsAleatorio)
        {
            filas = filas.Where(t => t.Signo == _signoFiltro.NombreCaps).ToList();
        }

        Tiendas = new ObservableCollection<TiendaDisponible>(filas);
    }

    /// <summary>
    /// Al volver de la pantalla del carrito (pantalla 11): las filas ya
    /// reflejan sus cambios (TiendaDisponible es ObservableObject), solo
    /// refresca los derivados del badge/overlay.
    /// </summary>
    public void AlAparecer()
    {
        NotificarDerivadasCantidad();
    }

    /// <summary>
    /// Botón carrito de una fila: abre el overlay 10.x (Cantidad de
    /// Cachitos) con esa fila en contexto. Los "disponibles" del diálogo
    /// se derivan de la propia fila, sin datos extra.
    /// Anti tap-fantasma: se ignora durante MsIgnorarTrasCarga ms tras
    /// la carga de la pantalla.
    /// </summary>
    [RelayCommand]
    private void CarritoTienda(TiendaDisponible? tienda)
    {
        if (tienda is null)
        {
            return;
        }

        if ((DateTime.UtcNow - _cargadoEnUtc).TotalMilliseconds < MsIgnorarTrasCarga)
        {
            return;
        }

        TiendaSeleccionada = tienda;
        CantidadTexto = string.Empty;
        CantidadConfirmada = 0;
        TecladoVisible = true;
        ToastCantidadVisible = false;
        _abiertoEnUtc = DateTime.UtcNow;
        CantidadDialogoVisible = true;
        NotificarDerivadasCantidad();
    }

    /// <summary>
    /// Tecla numérica del teclado 10.x: agrega dígito al campo (máx. 2).
    /// Un "0" inicial se reemplaza por el dígito capturado (evita
    /// campos "05").
    /// </summary>
    [RelayCommand]
    private void Tecla(string? digito)
    {
        if (!CantidadDialogoVisible || string.IsNullOrEmpty(digito))
        {
            return;
        }

        if (CantidadTexto.Length >= 2)
        {
            return;
        }

        CantidadTexto = CantidadTexto == "0" ? digito : CantidadTexto + digito;
    }

    /// <summary>
    /// Tecla "Borrar" del teclado (fila inferior): elimina el último
    /// dígito; el campo puede quedar vacío (vuelve a capturar desde 0).
    /// </summary>
    [RelayCommand]
    private void BorrarDigito()
    {
        if (!CantidadDialogoVisible)
        {
            return;
        }

        if (CantidadTexto.Length > 0)
        {
            CantidadTexto = CantidadTexto[..^1];
        }
    }

    /// <summary>
    /// Tecla "Realizado" del teclado: OCULTA el teclado (el diálogo
    /// queda junto a la fila, con "Aceptar" a la vista) y muestra el
    /// toast inferior con la cantidad capturada (mockup 10.2: campo
    /// vacío + toast "AQUÍ ELEGIRÁS 5 DE 16 / DISPONIBLES").
    /// NO aplica la cantidad: eso lo hace Aceptar (o Cancelar descarta).
    /// </summary>
    [RelayCommand]
    private void Realizado()
    {
        if (!CantidadDialogoVisible)
        {
            return;
        }

        if (!int.TryParse(CantidadTexto, out var cantidad) || cantidad <= 0)
        {
            return;
        }

        CantidadConfirmada = cantidad;
        TecladoVisible = false;
        ToastCantidadVisible = true;
        NotificarDerivadasCantidad();
    }

    /// <summary>
    /// Botón Aceptar del diálogo: aplica la cantidad capturada a la fila
    /// (el registro pasa de "0/20" a "n/20", Seleccionadas de la tienda)
    /// y cierra el overlay. Si el campo quedó vacío tras "Realizado",
    /// usa la cantidad confirmada por el teclado.
    /// </summary>
    [RelayCommand]
    private void AceptarCantidad()
    {
        var cantidad = int.TryParse(CantidadTexto, out var capturada) && capturada > 0
            ? capturada
            : CantidadConfirmada;

        if (TiendaSeleccionada is not null && cantidad > 0)
        {
            TiendaSeleccionada.Seleccionadas = cantidad;
        }

        CerrarOverlayCantidad();
    }

    /// <summary>
    /// Botón Cancelar del diálogo (y toque sobre el scrim): cierra el
    /// overlay sin aplicar nada. El toque sobre el scrim se ignora
    /// durante MsIgnorarCierreTrasAbrir ms tras abrir (tap fantasma).
    /// </summary>
    [RelayCommand]
    private void CancelarCantidad()
    {
        if ((DateTime.UtcNow - _abiertoEnUtc).TotalMilliseconds < MsIgnorarCierreTrasAbrir)
        {
            return;
        }

        CerrarOverlayCantidad();
    }

    private void CerrarOverlayCantidad()
    {
        CantidadDialogoVisible = false;
        TecladoVisible = false;
        ToastCantidadVisible = false;
        CantidadConfirmada = 0;
        TiendaSeleccionada = null;
        NotificarDerivadasCantidad();
    }

    /// <summary>
    /// Botón carrito del header: navega a la pantalla del carrito de
    /// compras (pantalla 11) con el sorteo en curso.
    /// </summary>
    [RelayCommand]
    private Task CarritoHeaderAsync()
    {
        if (Shell.Current is null || _sorteoId is null || _tipoSorteoId is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(
            $"{nameof(Views.CarritoComprasPage)}?sorteoId={_sorteoId}&tipoSorteoId={_tipoSorteoId}");
    }

    /// <summary>
    /// Abre/cierra el selector de signos (solo sorteos zodiaco).
    /// Durante los primeros MsIgnorarToggleTrasCarga ms tras la carga
    /// se ignora el toggle: defensa contra el "tap fantasma" que atraviesa
    /// la transición de navegación y golpearía la fila filtro recién
    /// creada, abriendo el selector sin que el usuario lo pida.
    /// </summary>
    [RelayCommand]
    private void ToggleSelectorSigno()
    {
        if (!EsZodiaco)
        {
            return;
        }

        if ((DateTime.UtcNow - _cargadoEnUtc).TotalMilliseconds < MsIgnorarToggleTrasCarga)
        {
            return;
        }

        SelectorSignoVisible = !SelectorSignoVisible;
    }

    /// <summary>
    /// MsIgnorarToggleTrasCarga renombrado: ver ToggleSelectorSigno.
    /// </summary>
    private const int MsIgnorarToggleTrasCarga = 600;

    /// <summary>
    /// Cierra el selector sin cambiar el filtro: se invoca al tocar
    /// fuera de las opciones (fondo del overlay), incluidos los espacios
    /// entre opciones.
    /// </summary>
    [RelayCommand]
    private void CerrarSelectorSigno()
    {
        SelectorSignoVisible = false;
    }

    /// <summary>
    /// Selección de un signo del selector: actualiza la fila filtro,
    /// cierra el selector y aplica el filtro a la lista.
    /// "Signo Aleatorio" limpia el filtro.
    /// </summary>
    [RelayCommand]
    private void SeleccionarSigno(SignoZodiaco? signo)
    {
        if (signo is null)
        {
            return;
        }

        _signoFiltro = signo;
        SignoFiltroTexto = signo.Nombre;
        SelectorSignoVisible = false;
        AplicarFiltros();
    }

    /// <summary>
    /// Retrocede a la pantalla anterior (seleccionar ciudad, pantalla 8).
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
