using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Models.Responses;
using MiCachito.Mobile.Services.Scanning;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Estado en memoria del flujo Devolución (mockups 6.x) + envío REAL de
/// la devolución (F4): los boletos se capturan escaneando su QR con el
/// parser existente y se envían a POST api/mobile/ventas/devolver al
/// GUARDAR (contexto de dotación para desambiguar: la combinación
/// numero_billete+serie+fraccion se repite entre dotaciones 4024/4025).
///
/// El backend valida pertenencia al billetero de la sesión, estado
/// 'asignado', guard de dotación cerrada y crea el movimiento
/// 'pendiente' con estado LIVE inmediato; la liquidación administrativa
/// queda en el flujo Desktop (docs/NOTAS_DEVOLUCION.md, P9).
/// </summary>
public class DevolucionService
{
    /// <summary>Equivalencias en cachitos por modo (Cachito=1, Tira=5, Serie=20).</summary>
    public const int EquivCachito = 1;
    public const int EquivTira = 5;
    public const int EquivSerie = 20;

    private readonly MobileVentasService _ventas;
    private readonly List<Devolucion> _devoluciones = new();
    private readonly List<FilaDesglose> _enCurso = new();

    /// <summary>
    /// Constructor con el servicio de ventas real (F4): el envío de la
    /// devolución usa POST api/mobile/ventas/devolver.
    /// </summary>
    public DevolucionService(MobileVentasService ventas)
    {
        _ventas = ventas;
    }

    /// <summary>
    /// Dotación elegida para la devolución en curso (null = sin elegir).
    /// Real desde F4 (GET api/mobile/ventas/sorteos, entidad de Vender).
    /// </summary>
    public DotacionDisponible? SorteoEnCurso { get; private set; }

    /// <summary>Modo de captura activo de la devolución en curso (para el escáner).</summary>
    public ModoCapturaDevolucion ModoActivo { get; private set; } = ModoCapturaDevolucion.Series;

    /// <summary>Fija el modo activo (lo hace Nueva Devolución al navegar al escáner).</summary>
    public void FijarModo(ModoCapturaDevolucion modo) => ModoActivo = modo;

    /// <summary>Folio consecutivo LOCAL de la devolución en curso (solo header; el folio real llega del backend).</summary>
    public int FolioEnCurso { get; private set; }

    /// <summary>Devoluciones registradas (GUARDAR), más reciente primero.</summary>
    public IReadOnlyList<Devolucion> Devoluciones => _devoluciones;

    /// <summary>Filas capturadas de la devolución en curso.</summary>
    public IReadOnlyList<FilaDesglose> FilasEnCurso => _enCurso;

    public int ContadorCachitos =>
        _enCurso.Count(f => f.Modo == ModoCapturaDevolucion.Cachitos);

    public int ContadorTiras =>
        _enCurso.Count(f => f.Modo == ModoCapturaDevolucion.Tiras);

    public int ContadorSeries =>
        _enCurso.Count(f => f.Modo == ModoCapturaDevolucion.Series);

    /// <summary>Inicia una devolución sobre la dotación elegida (6.1 real).</summary>
    public void Iniciar(DotacionDisponible sorteo)
    {
        SorteoEnCurso = sorteo;
        FolioEnCurso = _devoluciones.Count + 1;
        _enCurso.Clear();
    }

    /// <summary>Sorteo en curso para el header "Nueva Devolución - N".</summary>
    public string TituloEnCurso =>
        SorteoEnCurso is null ? "Nueva Devolución" : $"Nueva Devolución - {FolioEnCurso}";

    /// <summary>
    /// Procesa una cadena cruda en el modo dado: parsea (parser existente),
    /// dedupe, valida la fecha del cachito contra la dotación elegida y
    /// agrega la fila (guardando los campos que el backend necesita:
    /// fracción numérica, signo, serie y dotación). Devuelve (ok, mensaje).
    /// </summary>
    public (bool Ok, string Mensaje) Capturar(string cadena, ModoCapturaDevolucion modo)
    {
        if (SorteoEnCurso is null)
        {
            return (false, "Selecciona un sorteo antes de escanear.");
        }

        BilleteParseado? p = BilleteParser.ParsearCadena(cadena);
        if (p is null)
        {
            return (false, "Código no reconocido. Verifica el boleto e intenta de nuevo.");
        }

        if (!p.EsLoteriaNacional)
        {
            return (false, "La devolución LOTENAL solo acepta boletos de Lotería Nacional.");
        }

        if (_enCurso.Any(f => f.CodigoCompleto == p.CodigoCompleto))
        {
            return (false, "Ese boleto ya fue capturado en esta devolución.");
        }

        // Validación de fecha del mockup 6.2 "Escanear series": la fecha del
        // cachito debe corresponder con la de la dotación elegida.
        string? fechaDotacion = SorteoEnCurso.FechaCelebracion?.ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(p.FechaSorteo)
            && !string.IsNullOrEmpty(fechaDotacion)
            && p.FechaSorteo != fechaDotacion)
        {
            return (false, "La fecha del cachito no corresponde con la fecha del sorteo seleccionado");
        }

        _enCurso.Add(new FilaDesglose
        {
            Modo = modo,
            CodigoCompleto = p.CodigoCompleto,
            Billete = p.NumeroBillete ?? "-",
            Cantidad = modo switch
            {
                ModoCapturaDevolucion.Cachitos => EquivCachito,
                ModoCapturaDevolucion.Tiras => EquivTira,
                _ => EquivSerie,
            },
            Vigesimo = p.Vigesimo is int v ? $"{v:00}" : "-",
            Serie = p.EsZodiaco ? (p.SignoNombre ?? "-") : (p.Serie ?? "-"),
            FechaSorteoCodigo = p.FechaSorteo,
            // Campos para el POST (F4): el backend desambigua por estos
            // más la dotación del contexto (numero_sorteo).
            FraccionNum = p.Vigesimo,
            SignoCodigo = p.SignoCodigo,
            SerieFisica = p.Serie,
            NumeroSorteo = SorteoEnCurso.NumeroSorteo,
            IdSorteo = SorteoEnCurso.IdSorteo,
        });

        return (true, string.Empty);
    }

    /// <summary>
    /// GUARDAR (F4, REAL): envía los boletos capturados a
    /// POST api/mobile/ventas/devolver (items con numero_billete, serie,
    /// fracción, signo y la dotación del contexto) y registra la
    /// devolución local con el FOLIO REAL del movimiento.
    /// </summary>
    public async Task<Devolucion> GuardarAsync()
    {
        DotacionDisponible? sorteo = SorteoEnCurso;
        if (sorteo is null || _enCurso.Count == 0)
        {
            throw new InvalidOperationException("No hay devolución en curso.");
        }

        List<ItemDevolucionApi> items = _enCurso.Select(f => new ItemDevolucionApi
        {
            NumeroBillete = f.Billete == "-" ? string.Empty : f.Billete,
            Serie = f.SerieFisica,
            Fraccion = f.FraccionNum,
            SignoCodigo = f.SignoCodigo,
            NumeroSorteo = f.NumeroSorteo,
        }).ToList();

        DevolucionCreadaApi dev = await _ventas.DevolverAsync(items).ConfigureAwait(true);

        var registro = new Devolucion
        {
            Folio = dev.IdMovimiento,
            FolioTexto = dev.Folio,
            IdSorteo = sorteo.IdSorteo,
            NombreSorteo = string.IsNullOrEmpty(sorteo.NumeroSorteo)
                ? sorteo.NombreProducto
                : $"{sorteo.NombreProducto} {sorteo.NumeroSorteo}",
            FechaSorteo = sorteo.FechaSorteoTexto ?? "-",
            Estatus = dev.Estatus,
        };
        registro.Filas.AddRange(_enCurso);
        _devoluciones.Insert(0, registro);
        _enCurso.Clear();
        SorteoEnCurso = null;
        return registro;
    }

    /// <summary>CANCELAR (mantener presionado): descarta las filas en curso.</summary>
    public void Cancelar()
    {
        _enCurso.Clear();
        SorteoEnCurso = null;
    }
}
