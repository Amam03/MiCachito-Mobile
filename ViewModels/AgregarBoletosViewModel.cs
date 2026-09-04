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
/// Los dos carritos (por fila y del header) son SOLO visuales por ahora:
/// su función se define en las pantallas 10.x/11 (cantidad de boletos y
/// carrito de compras).
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

    /// <summary>
    /// Ventana (ms) tras la carga inicial en la que se ignora el toggle
    /// del selector de signo: en Android, el tap que dispara la navegación
    /// (elección de ciudad en la pantalla 8) puede "atravesar" la
    /// transición y golpear la fila filtro recién creada de esta pantalla,
    /// abriendo el selector sin que el usuario lo haya pedido.
    /// </summary>
    private const int MsIgnorarToggleTrasCarga = 600;

    private int? _sorteoId;
    private int? _tipoSorteoId;
    private int? _ciudadId;
    private bool _cargado;
    private DateTime _cargadoEnUtc = DateTime.MinValue;
    private SignoZodiaco _signoFiltro = SignosZodiacoData.SignoAleatorio;

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
    /// Botón carrito de una fila. UI-only: sin lógica por ahora.
    /// Futuro: navegar a la pantalla 10.x (cantidad de boletos) de esa
    /// tienda.
    /// </summary>
    [RelayCommand]
    private Task CarritoTiendaAsync(TiendaDisponible? tienda)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Botón carrito del header. UI-only: sin lógica por ahora.
    /// Futuro: navegar a la pantalla del carrito de compras.
    /// </summary>
    [RelayCommand]
    private Task CarritoHeaderAsync()
    {
        return Task.CompletedTask;
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
