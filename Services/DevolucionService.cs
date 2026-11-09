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
/// 'asignado' y guard de dotación cerrada, y crea la captura 'pendiente'
/// SIN efecto en inventario: los billetes siguen 'asignado' hasta que
/// Escritorio la procesa (Entrega a Billeteros > Devolución > Billetero).
/// Un billete con captura pendiente no puede venderse (409).
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

    /// <summary>
    /// Carga el historial PERSISTENTE del backend (GET
    /// api/mobile/devoluciones, fecha descendente) y lo fusiona con los
    /// registros en memoria: los guardados en esta sesión (que el backend
    /// ya tiene) se sustituyen por la fila del servidor (misma fuente de
    /// verdad), y cualquier otra devolución del billetero —incluidas las
    /// de sesiones o días anteriores y las registradas desde
    /// Escritorio— se reconstruye. Sin duplicados por id_movimiento.
    ///
    /// Requiere red: la lista en memoria se CONSERVA si la carga falla
    /// (la pantalla muestra el error y el reintento re-entra).
    /// </summary>
    public async Task CargarHistorialAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DevolucionHistorialApi> historial =
            await _ventas.CargarDevolucionesAsync(cancellationToken).ConfigureAwait(true);

        // El backend es la fuente de verdad: reemplaza la lista local.
        // Los registros locales recién guardados tienen IdMovimiento
        // real (devuelto por POST ventas/devolver), así que la fusión por
        // id no duplica nada.
        _devoluciones.Clear();
        foreach (DevolucionHistorialApi h in historial)
        {
            _devoluciones.Add(Mapear(h));
        }
    }

    /// <summary>Fila del historial → registro de la lista (mockup 6).</summary>
    private static Devolucion Mapear(DevolucionHistorialApi h)
    {
        // Fecha de celebración del sorteo en texto corto es-MX ("24 sep").
        string fechaSorteo = "-";
        if (DateOnly.TryParse(
                h.FechaSorteo,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateOnly fs))
        {
            fechaSorteo = fs.ToString("dd MMM", new System.Globalization.CultureInfo("es-MX"));
        }

        return new Devolucion
        {
            Folio = h.IdMovimiento,
            FolioTexto = h.Folio,
            IdSorteo = h.IdSorteo,
            NombreSorteo = string.IsNullOrEmpty(h.Dotacion)
                ? (h.NombreProducto ?? h.Sorteo ?? "-")
                : $"{h.Dotacion} - {h.NombreProducto ?? "-"}",
            FechaSorteo = fechaSorteo,
            Estatus = h.Estatus,
            // Fecha del movimiento (no la de "ahora"): la fila del listado
            // muestra cuándo se registró la devolución.
            RegistradaEnUtc = DateOnly.TryParse(
                h.FechaMovimiento,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateOnly fm)
                ? fm.ToDateTime(TimeOnly.MinValue)
                : DateTime.UtcNow,
            TotalCachitos = h.TotalCachitos,
        };
    }

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
        // Si ya había un latido de OTRO sorteo, se cierra (y se limpia en el backend).
        if (Volatile.Read(ref _latidoActivo) == 1 && _idSorteoLatido != sorteo.IdSorteo)
        {
            DetenerEnCaptura();
        }

        SorteoEnCurso = sorteo;
        FolioEnCurso = _devoluciones.Count + 1;
        _enCurso.Clear();
        Volatile.Write(ref _filasEnCursoSnapshot, 0);

        // Ticket 2-22: desde aquí el billetero está "en captura" (amarillo en el
        // Monitor de Escritorio) hasta que guarde, cancele o salga del flujo.
        IniciarLatido(sorteo.IdSorteo, sorteo.NumeroSorteo);
    }

    // ── Ticket 2-22: latido "captura en curso" (Monitor de Escritorio) ──────
    //
    // Mientras hay una devolución sin guardar se avisa al backend cada ~20 s
    // (y tras cada boleto capturado). El servidor caduca el aviso a los 60 s,
    // así que si la app se cierra o pierde red el amarillo se apaga solo. Todo
    // es MEJOR ESFUERZO: jamás bloquea el escaneo ni muestra errores al usuario.
    // La lista de filas solo se toca en el hilo de UI: el temporizador lee un
    // contador (snapshot) para no recorrerla desde otro hilo.

    private const int LatidoIntervaloSegundos = 20;
    private const int LatidoInactividadMaximaMinutos = 5;

    private System.Threading.Timer? _latidoTimer;
    private int _latidoActivo;
    private int _latidoEnVuelo;
    private int _idSorteoLatido;
    private string? _dotacionLatido;
    private int _filasEnCursoSnapshot;
    private long _ultimaActividadTicks = DateTime.UtcNow.Ticks;

    /// <summary>
    /// Marca actividad en el flujo (las pantallas de captura la llaman al
    /// aparecer). Sin actividad durante 5 min el latido se detiene solo: evita
    /// un amarillo falso si el billetero salió del flujo sin volver al listado.
    /// </summary>
    public void RegistrarActividad() =>
        Interlocked.Exchange(ref _ultimaActividadTicks, DateTime.UtcNow.Ticks);

    private void IniciarLatido(int idSorteo, string? numeroSorteo = null)
    {
        _latidoTimer?.Dispose();
        _idSorteoLatido = idSorteo;
        _dotacionLatido = numeroSorteo;
        Volatile.Write(ref _latidoActivo, 1);
        RegistrarActividad();
        _latidoTimer = new System.Threading.Timer(
            _ => _ = TickLatidoAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(LatidoIntervaloSegundos));
    }

    private async Task TickLatidoAsync()
    {
        if (Volatile.Read(ref _latidoActivo) == 0)
        {
            return;
        }

        var inactivo = TimeSpan.FromTicks(
            DateTime.UtcNow.Ticks - Interlocked.Read(ref _ultimaActividadTicks));
        if (inactivo > TimeSpan.FromMinutes(LatidoInactividadMaximaMinutos))
        {
            DetenerEnCaptura();
            return;
        }

        await EnviarLatidoAsync().ConfigureAwait(false);
    }

    private async Task EnviarLatidoAsync()
    {
        if (Volatile.Read(ref _latidoActivo) == 0
            || Interlocked.Exchange(ref _latidoEnVuelo, 1) == 1)
        {
            return;
        }

        try
        {
            await _ventas.ReportarCapturaEnCursoAsync(
                _idSorteoLatido, Volatile.Read(ref _filasEnCursoSnapshot), _dotacionLatido).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Mejor esfuerzo: sin red o backend antiguo (404) no pasa nada.
        }
        finally
        {
            Volatile.Write(ref _latidoEnVuelo, 0);
        }
    }

    private void PausarLatido()
    {
        Volatile.Write(ref _latidoActivo, 0);
        _latidoTimer?.Dispose();
        _latidoTimer = null;
    }

    private async Task CancelarBorradorAsync(int idSorteo, string? numeroSorteo = null)
    {
        try
        {
            await _ventas.CancelarCapturaEnCursoAsync(idSorteo, numeroSorteo).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Mejor esfuerzo: si falla, el aviso caduca solo (TTL del servidor).
        }
    }

    /// <summary>
    /// Detiene el latido y borra el borrador en el backend (Monitor sin amarillo).
    /// Se llama al cancelar, al volver al listado de sorteos sin guardar o al
    /// agotarse la inactividad. Sin latido activo no hace nada.
    /// </summary>
    public void DetenerEnCaptura()
    {
        bool estabaActivo = Interlocked.Exchange(ref _latidoActivo, 0) == 1;
        _latidoTimer?.Dispose();
        _latidoTimer = null;
        if (estabaActivo)
        {
            _ = CancelarBorradorAsync(_idSorteoLatido, _dotacionLatido);
        }
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

        // Subcódigo: el boleto debe ser del sorteo elegido. Sin subcódigo
        // configurado en el sorteo no se valida (mismo criterio permisivo que
        // Desktop). Es la identificación confiable del sorteo del boleto.
        if (!string.IsNullOrEmpty(SorteoEnCurso.SubcodigoSorteo)
            && !string.IsNullOrEmpty(p.SubCodigo)
            && p.SubCodigo != SorteoEnCurso.SubcodigoSorteo)
        {
            return (false, "El boleto no corresponde al sorteo seleccionado");
        }

        // Validación de fecha del mockup 6.2 "Escanear series": distingue
        // dotaciones del mismo producto (billete+serie+fracción se repiten
        // entre 4024/4025). Solo se aplica cuando la fecha es la de la
        // DOTACIÓN (recepción); la fecha del maestro del sorteo no es de la
        // edición (Gordito: maestro 2025-12-25, boleto 24/12/2026) y NO debe
        // rechazar un boleto válido. La pertenencia al billetero, el estatus
        // y la dotación los valida además el backend.
        string? fechaDotacion = SorteoEnCurso.FechaCelebracion?.ToString("yyyy-MM-dd");
        if (SorteoEnCurso.FechaEsDeDotacion
            && !string.IsNullOrEmpty(p.FechaSorteo)
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

        // Ticket 2-22: boleto nuevo => actividad y latido inmediato (Monitor).
        Volatile.Write(ref _filasEnCursoSnapshot, _enCurso.Count);
        RegistrarActividad();
        _ = EnviarLatidoAsync();

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

        // Ticket 2-22: durante el guardado se pausa el latido (un latido tardío
        // recrearía el amarillo tras guardar). Si el guardado falla, se reanuda:
        // la captura se conserva para corregir y reintentar.
        PausarLatido();
        DevolucionCreadaApi dev;
        try
        {
            dev = await _ventas.DevolverAsync(items).ConfigureAwait(true);
        }
        catch
        {
            if (SorteoEnCurso is not null)
            {
                IniciarLatido(sorteo.IdSorteo, sorteo.NumeroSorteo);
            }
            throw;
        }

        // Guardada: el backend ya borró el borrador; se limpia por si un latido en
        // vuelo lo recreó (la devolución guardada pasa a CAPTURADA/rojo).
        _ = CancelarBorradorAsync(sorteo.IdSorteo, sorteo.NumeroSorteo);

        var registro = new Devolucion
        {
            Folio = dev.IdMovimiento,
            FolioTexto = dev.Folio,
            IdSorteo = sorteo.IdSorteo,
            NombreSorteo = string.IsNullOrEmpty(sorteo.NumeroSorteo)
                ? sorteo.NombreProducto
                : $"{sorteo.NumeroSorteo} - {sorteo.NombreProducto}",
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
        DetenerEnCaptura();
        _enCurso.Clear();
        SorteoEnCurso = null;
        Volatile.Write(ref _filasEnCursoSnapshot, 0);
    }
}
