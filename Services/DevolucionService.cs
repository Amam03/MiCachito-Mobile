using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services.Scanning;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Estado en memoria del flujo Devolución (mockups 6.x). Fase
/// solo-interfaz: nada se persiste, no hay seeds; los contadores
/// arrancan en 0 y solo cambian al capturar códigos reales por el
/// escáner/entrada manual (fuente real: backend, ver
/// docs/NOTAS_DEVOLUCION.md).
/// </summary>
public class DevolucionService
{
    /// <summary>Equivalencias en cachitos por modo (Cachito=1, Tira=5, Serie=20).</summary>
    public const int EquivCachito = 1;
    public const int EquivTira = 5;
    public const int EquivSerie = 20;

    private readonly List<Devolucion> _devoluciones = new();
    private readonly List<FilaDesglose> _enCurso = new();

    /// <summary>Sorteo activo elegido para la devolución en curso (null = sin elegir).</summary>
    public SorteoActivoLotenal? SorteoEnCurso { get; private set; }

    /// <summary>Modo de captura activo de la devolución en curso (para el escáner).</summary>
    public ModoCapturaDevolucion ModoActivo { get; private set; } = ModoCapturaDevolucion.Series;

    /// <summary>Fija el modo activo (lo hace Nueva Devolución al navegar al escáner).</summary>
    public void FijarModo(ModoCapturaDevolucion modo) => ModoActivo = modo;

    /// <summary>Folio consecutivo de la devolución en curso (asignado al iniciar).</summary>
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

    /// <summary>Inicia una devolución: fija el sorteo y asigna folio consecutivo.</summary>
    public void Iniciar(SorteoActivoLotenal sorteo)
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
    /// dedupe, valida la fecha del cachito contra el sorteo elegido y agrega
    /// la fila. Devuelve (ok, mensaje) para el aviso de la pantalla de escaneo.
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

        if (_enCurso.Any(f => f.CodigoCompleto == p.CodigoCompleto))
        {
            return (false, "Ese boleto ya fue capturado en esta devolución.");
        }

        // Validación de fecha del mockup 6.2 "Escanear series": la fecha del
        // cachito debe corresponder con la del sorteo seleccionado.
        if (!string.IsNullOrEmpty(p.FechaSorteo)
            && p.FechaSorteo != SorteoEnCurso.FechaSorteoCodigo)
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
        });

        return (true, string.Empty);
    }

    /// <summary>GUARDAR: registra la devolución con las filas en curso (sin validación de vacío, UI-only).</summary>
    public Devolucion Guardar()
    {
        var dev = new Devolucion
        {
            Folio = FolioEnCurso,
            IdSorteo = SorteoEnCurso?.IdSorteo ?? 0,
            NombreSorteo = SorteoEnCurso?.NombreCorto ?? "-",
            FechaSorteo = SorteoEnCurso?.FechaSorteoTexto ?? "-",
        };
        dev.Filas.AddRange(_enCurso);
        _devoluciones.Insert(0, dev);
        _enCurso.Clear();
        SorteoEnCurso = null;
        return dev;
    }

    /// <summary>CANCELAR (mantener presionado): descarta las filas en curso.</summary>
    public void Cancelar()
    {
        _enCurso.Clear();
        SorteoEnCurso = null;
    }
}
