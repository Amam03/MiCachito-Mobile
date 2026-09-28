using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Agregar Boletos" (pantallas 9.1 / 9.2 /
/// 10.x). Aparece tras seleccionar la ciudad (pantalla 8) en el flujo
/// de venta LOTENAL.
///
/// F2 CONEXIÓN REAL: las filas son las SERIES con fracciones libres
/// de la dotación elegida (GET api/mobile/ventas/billetes con los
/// parámetros id_sorteo y numero_sorteo) — inventario LIVE de
/// billetes_loteria, sin inventario paralelo. La clave de la dotación
/// llega como "id_sorteo|numero_sorteo" (ej. "41|4024"); las
/// dotaciones 4024 y 4025 son consultas distintas con billetes
/// separados.
///
/// Recibe vía QueryProperty (navegación Shell):
///   - sorteoId: CLAVE de la dotación ("41|4024"; "41|" = legacy)
///   - tipoSorteoId: tipo de sorteo (pantalla 6)
///   - ciudadId: ciudad elegida (0 = "Cualquier ciudad"; picker
///     operativo, P3 pendiente — el backend no filtra por ciudad)
///
/// VARIACIÓN ZODIACO (tipos 3 y 4): cada fila añade la línea de SIGNO
/// (signo_nombre real de billetes_loteria) y la fila filtro "Signo
/// Aleatorio ▼" filtra las series reales por su signo.
///
/// PANTALLA 10.x (Cantidad de Cachitos): overlay con diálogo + teclado
/// numérico (mismo flujo que el mock): Aceptar aplica la cantidad a la
/// fila; los "disponibles" del diálogo derivan de TotalDisponible de la
/// SERIE seleccionada. La selección es ESTADO LOCAL (la venta real F3
/// valida contra el backend).
///
/// ESTADO COMPARTIDO: las series viven en SesionVentaLotenal (mismas
/// instancias entre 9.x y el carrito 11): mutar Seleccionadas se
/// refleja en ambas sin recargar.
/// </summary>
[QueryProperty(nameof(SorteoIdStr), "sorteoId")]
[QueryProperty(nameof(TipoSorteoIdStr), "tipoSorteoId")]
[QueryProperty(nameof(CiudadIdStr), "ciudadId")]
public partial class AgregarBoletosViewModel : BaseViewModel
{
    /// <summary>Ids de los tipos de sorteo zodiacales (ver SorteosLotenalData).</summary>
    private const int TipoZodiaco = 3;
    private const int TipoZodiacoEspecial = 4;

    private readonly MobileVentasService _servicio;

    /// <summary>Series con fracciones libres de la dotación (filas 9.x).</summary>
    [ObservableProperty]
    private ObservableCollection<SerieDisponible> _tiendas = new();

    /// <summary>Catálogo de signos del selector (estático, 12 signos).</summary>
    [ObservableProperty]
    private ObservableCollection<SignoZodiaco> _signos = new();

    /// <summary>True cuando el selector de signos está desplegado (solo zodiaco).</summary>
    [ObservableProperty]
    private bool _selectorSignoVisible;

    /// <summary>Texto de la fila filtro: "Signo Aleatorio" o el signo elegido.</summary>
    [ObservableProperty]
    private string _signoFiltroTexto = "Signo Aleatorio";

    /// <summary>
    /// True para Zodiaco (3) y Zodiaco Especial (4): activa la fila
    /// filtro de signo, la línea de signo por fila y el selector.
    /// </summary>
    [ObservableProperty]
    private bool _esZodiaco;

    // ============ Estados de carga (patrón Gestión) ============

    /// <summary>True mientras carga la dotación (overlay "Procesando").</summary>
    [ObservableProperty]
    private bool _cargando;

    /// <summary>Mensaje de error de la carga (reintentar con botón).</summary>
    [ObservableProperty]
    private string _mensajeError = string.Empty;

    /// <summary>True cuando hay error visible.</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>True cuando la dotación cargó sin series (estado vacío).</summary>
    [ObservableProperty]
    private bool _sinSeries;

    /// <summary>Dotación activa (encabezado real del backend).</summary>
    [ObservableProperty]
    private DotacionDisponible? _dotacion;

    // ============ Pantalla 10.x: Cantidad de Cachitos ============

    /// <summary>True cuando el overlay 10.x (diálogo + teclado) está abierto.</summary>
    [ObservableProperty]
    private bool _cantidadDialogoVisible;

    /// <summary>True cuando el teclado numérico está visible (Realizado lo oculta).</summary>
    [ObservableProperty]
    private bool _tecladoVisible;

    /// <summary>Fila cuyo carrito se tocó (serie en contexto).</summary>
    [ObservableProperty]
    private SerieDisponible? _tiendaSeleccionada;

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
    /// del diálogo de cantidad: defensa anti "tap fantasma".
    /// </summary>
    private const int MsIgnorarTrasCarga = 600;

    /// <summary>
    /// Ventana (ms) tras ABRIR el overlay 10.x en la que se ignora el
    /// toque sobre el scrim (tap fantasma).
    /// </summary>
    private const int MsIgnorarCierreTrasAbrir = 600;

    private string? _sorteoClave;
    private int? _tipoSorteoId;
    private int? _ciudadId;
    private bool _cargado;
    private DateTime _cargadoEnUtc = DateTime.MinValue;
    private SignoZodiaco _signoFiltro = SignosZodiacoData.SignoAleatorio;

    /// <summary>
    /// Series COMPLETAS de la dotación (fuente de los filtros): el
    /// filtro de signo corre siempre sobre esta lista, nunca sobre la
    /// ya filtrada (reasignar Tiendas mutaría _tiendas).
    /// </summary>
    private IReadOnlyList<SerieDisponible> _seriesTodas = [];

    /// <summary>
    /// Disponibles de la fila seleccionada (junto al campo del diálogo
    /// 10.x). Derivado de la propia SERIE; vacío sin selección.
    /// </summary>
    public string DisponiblesTexto => TiendaSeleccionada is null
        ? string.Empty
        : TiendaSeleccionada.TotalDisponible.ToString();

    /// <summary>
    /// Línea 1 del toast (mockup 10.2): "AQUÍ ELEGIRÁS {n} DE {disp}",
    /// con n = cantidad confirmada y disp = disponibles de la serie.
    /// </summary>
    public string ToastLinea1 =>
        $"AQUÍ ELEGIRÁS {CantidadConfirmada} DE {DisponiblesTexto}";

    /// <summary>
    /// Series con selección en la dotación completa (no solo la lista
    /// filtrada): coincide con los registros que muestra el carrito.
    /// Corre sobre SesionVentaLotenal (las MISMAS instancias que la
    /// lista 9.x), no sobre la colección ObservableProperty.
    /// </summary>
    private int TiendasConSeleccion() =>
        SesionVentaLotenal.Series.Count(t => t.Seleccionadas > 0);

    /// <summary>
    /// Badge del carrito del header: SERIES con selección (registros
    /// del carrito), en dos dígitos ("00" sin registros).
    /// </summary>
    public string CarritoBadgeTexto => TiendasConSeleccion().ToString("00");

    /// <summary>
    /// El badge se muestra con el overlay abierto (mockups 10.x) o
    /// cuando ya hay registros en el carrito.
    /// </summary>
    public bool MostrarBadgeCarrito =>
        CantidadDialogoVisible || TiendasConSeleccion() > 0;

    /// <summary>
    /// CLAVE de la dotación ("41|4024"), recibida como string vía
    /// navegación Shell.
    /// </summary>
    public string? SorteoIdStr
    {
        get => _sorteoClave;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                _sorteoClave = value;
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
    /// Id de la ciudad seleccionada (0 = "Cualquier ciudad"), recibido
    /// como string vía navegación Shell. Picker operativo (P3): la
    /// consulta real no filtra por ciudad — muestra TODAS las series
    /// de la dotación.
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

    public AgregarBoletosViewModel(MobileVentasService servicio)
    {
        _servicio = servicio;
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
    /// Carga la dotación solo cuando los tres parámetros de navegación
    /// están disponibles (Shell los asigna en orden indeterminado).
    /// </summary>
    private void IntentarCargar()
    {
        if (_cargado || _sorteoClave is null || _tipoSorteoId is null || _ciudadId is null)
        {
            return;
        }

        _cargado = true;
        _ = CargarAsync();
    }

    /// <summary>
    /// Carga REAL de la dotación: GET api/mobile/ventas/billetes con la
    /// clave recibida (id_sorteo + numero_sorteo de la dotación). Las
    /// series compartidas se fijan en SesionVentaLotenal (el carrito 11
    /// las reconstruye de ahí). Estados: overlay al cargar, aviso rojo
    /// con reintento, vacío "Sin billetes disponibles".
    /// </summary>
    private async Task CargarAsync()
    {
        if (Cargando || _sorteoClave is null)
        {
            return;
        }

        Cargando = true;
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(HayError));
        try
        {
            // Clave "41|4024" → (id_sorteo=41, numero_sorteo="4024").
            string[] partes = _sorteoClave.Split('|', 2);
            if (partes.Length != 2 || !int.TryParse(partes[0], out int idSorteo))
            {
                MensajeError = "La dotación no es válida";
                OnPropertyChanged(nameof(HayError));
                return;
            }

            string? numeroSorteo = string.IsNullOrWhiteSpace(partes[1]) ? null : partes[1];

            DetalleDotacion detalle = await _servicio.BilletesDisponiblesAsync(
                idSorteo, numeroSorteo).ConfigureAwait(true);

            Dotacion = detalle.Dotacion;
            _seriesTodas = detalle.Series;
            Tiendas = new ObservableCollection<SerieDisponible>(detalle.Series);
            SesionVentaLotenal.Establecer(detalle.Dotacion, detalle.Series);

            _cargadoEnUtc = DateTime.UtcNow;
            SinSeries = detalle.Series.Count == 0;

            Signos = new ObservableCollection<SignoZodiaco>(SignosZodiacoData.ObtenerTodas());
            AplicarFiltros();
        }
        catch (TaskCanceledException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (OperationCanceledException)
        {
            MensajeError = "Consulta cancelada";
            OnPropertyChanged(nameof(HayError));
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            OnPropertyChanged(nameof(HayError));
        }
        catch (ApiException ex)
        {
            MensajeError = ex.Message;
            OnPropertyChanged(nameof(HayError));
        }
        finally
        {
            Cargando = false;
        }
    }

    /// <summary>
    /// Reintento manual tras error (aviso rojo de la carga).
    /// </summary>
    [RelayCommand]
    private Task ReintentarAsync() => CargarAsync();

    /// <summary>
    /// Aplica los filtros activos (signo) sobre las series de la
    /// dotación y refresca la lista. "Signo Aleatorio" = sin filtro.
    /// El filtro por ciudad NO aplica al material real (P3: la
    /// disponibilidad es del billetero, no por ciudad).
    /// </summary>
    private void AplicarFiltros()
    {
        // Fuente de los filtros: la lista COMPLETA de la dotación
        // (SesionVentaLotenal), nunca la ya filtrada.
        IEnumerable<SerieDisponible> filas = SesionVentaLotenal.Series;

        if (EsZodiaco && !_signoFiltro.EsAleatorio)
        {
            filas = filas.Where(t => t.Signo == _signoFiltro.NombreCaps);
        }

        Tiendas = new ObservableCollection<SerieDisponible>(filas);
    }

    /// <summary>
    /// Al volver del carrito (pantalla 11): las filas ya reflejan sus
    /// cambios (SerieDisponible es ObservableObject, mismas
    /// instancias), solo refresca los derivados del badge/overlay.
    /// </summary>
    public void AlAparecer()
    {
        NotificarDerivadasCantidad();
    }

    /// <summary>
    /// Botón carrito de una fila: abre el overlay 10.x con esa serie en
    /// contexto. Los "disponibles" del diálogo derivan de la propia
    /// fila. Anti tap-fantasma: se ignora durante MsIgnorarTrasCarga ms
    /// tras la carga.
    /// </summary>
    [RelayCommand]
    private void CarritoTienda(SerieDisponible? tienda)
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
    /// Tecla "Borrar" del teclado: elimina el último dígito.
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
    /// Tecla "Realizado": OCULTA el teclado y muestra el toast con la
    /// cantidad capturada (mockup 10.2). NO aplica la cantidad.
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
    /// Botón Aceptar del diálogo: aplica la cantidad a la serie y
    /// cierra el overlay. La selección es ESTADO LOCAL (F3 validará
    /// contra el backend al vender).
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
    /// overlay sin aplicar nada. El toque del scrim se ignora durante
    /// MsIgnorarCierreTrasAbrir ms tras abrir (tap fantasma).
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

    /// <summary>
    /// Cierra el overlay 10.x reseteando el diálogo y el teclado.
    /// </summary>
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
    /// Botón carrito del header: navega a la pantalla del carrito (11)
    /// con el tipo en curso.
    /// </summary>
    [RelayCommand]
    private Task CarritoHeaderAsync()
    {
        if (Shell.Current is null || _tipoSorteoId is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(
            $"{nameof(Views.CarritoComprasPage)}?tipoSorteoId={_tipoSorteoId}");
    }

    /// <summary>
    /// MsIgnorarToggleTrasCarga: ver ToggleSelectorSigno.
    /// </summary>
    private const int MsIgnorarToggleTrasCarga = 600;

    /// <summary>
    /// Abre/cierra el selector de signos (solo sorteos zodiaco).
    /// Durante los primeros MsIgnorarToggleTrasCarga ms tras la carga
    /// se ignora el toggle: defensa contra el "tap fantasma" que
    /// atraviesa la transición de navegación y golpearía la fila
    /// filtro recién creada.
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
    /// Cierra el selector sin cambiar el filtro: se invoca al tocar
    /// fuera de las opciones (fondo del overlay).
    /// </summary>
    [RelayCommand]
    private void CerrarSelectorSigno()
    {
        SelectorSignoVisible = false;
    }

    /// <summary>
    /// Selección de un signo del selector: actualiza la fila filtro,
    /// cierra el selector y aplica el filtro a la lista de series.
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
