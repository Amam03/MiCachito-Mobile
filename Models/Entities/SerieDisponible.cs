using CommunityToolkit.Mvvm.ComponentModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Serie de una dotación con fracciones LIVE libres (pantalla 9.x):
/// UNA fila por (numero_billete, serie[, signo]) tal como la agrupa
/// GET api/mobile/ventas/billetes. Con datos reales la fila de la
/// pantalla pasa de "tienda/ciudad" (mock) a SERIE: DisplayName
/// muestra "SERIE 01" (la ciudad quedó fuera del MVP, §P3).
///
/// ESTADO MUTABLE (pantallas 10.x / 11): Seleccionadas y
/// TotalDisponible cambian al aceptar cantidades / eliminar / vender
/// y notifican a la UI (mismo patrón que TiendaDisponible del mock:
/// la lista 9.x y el carrito comparten las MISMAS instancias).
///
/// FraccionesLibres conserva cada fracción con su id_billete exacto:
/// la venta (F3) será por id_billete. NO es inventario paralelo —
/// billetes_loteria vía backend es la fuente de verdad LIVE y la app
/// reconsulta al entrar a la dotación.
/// </summary>
public class SerieDisponible : ObservableObject
{
    /// <summary>
    /// Clave de la serie dentro de la dotación activa (almacén de
    /// sesión): el carrito localiza la fila por este id.
    /// </summary>
    public int IdSerie { get; init; }

    /// <summary>Número de billete tal cual lo agrupa el backend ("0102").</summary>
    public string Numero { get; init; } = string.Empty;

    /// <summary>Serie física de billetes_loteria ("01"); vacía = sin serie.</summary>
    public string Serie { get; init; } = string.Empty;

    /// <summary>
    /// Signo zodiacal en MAYÚSCULAS sin acentos (solo zodiaco,
    /// billetes_loteria.signo_nombre); null si no aplica.
    /// </summary>
    public string? Signo { get; init; }

    /// <summary>Fracciones libres de la serie (id_billete exacto, venta F3).</summary>
    public IReadOnlyList<FraccionLibre> FraccionesLibres { get; init; } = [];

    /// <summary>Precio por cachito de la dotación (backend).</summary>
    public decimal PrecioFraccion { get; init; }

    // ============ Estado mutable (pantallas 10.x / 11) ============

    private int _seleccionadas;

    /// <summary>
    /// Cachitos seleccionados de esta serie: se fija al pulsar Aceptar
    /// del diálogo 10.x y se limpia al Eliminar del carrito o Vender.
    /// </summary>
    public int Seleccionadas
    {
        get => _seleccionadas;
        set
        {
            if (SetProperty(ref _seleccionadas, value))
            {
                OnPropertyChanged(nameof(FraccionesDisplay));
            }
        }
    }

    private int _totalDisponible;

    /// <summary>
    /// Fracciones DISPONIBLES vigentes de la serie: inicia con el
    /// conteo del backend y DISMINUYE al Vender (estado local; F3 lo
    /// conecta a la venta real).
    /// </summary>
    public int TotalDisponible
    {
        get => _totalDisponible;
        set
        {
            if (SetProperty(ref _totalDisponible, value))
            {
                OnPropertyChanged(nameof(FraccionesDisplay));
            }
        }
    }

    /// <summary>Línea 1 de la fila 9.x: "{seleccionadas}/{disponibles}".</summary>
    public string FraccionesDisplay => $"{Seleccionadas}/{TotalDisponible}";

    /// <summary>True cuando la fila lleva línea de signo (sorteo zodiaco).</summary>
    public bool MostrarSigno => !string.IsNullOrEmpty(Signo);

    /// <summary>Línea 2 de la fila 9.x (real): "SERIE 01" / "SIN SERIE".</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Serie)
        ? "SIN SERIE"
        : $"SERIE {Serie}";
}

/// <summary>
/// Fracción libre individual de una serie (id_billete exacto de
/// billetes_loteria + número de fracción/vigésimo). La venta F3 será
/// por id_billete; F2 solo la preserva.
/// </summary>
public class FraccionLibre
{
    /// <summary>PK de la fila de billetes_loteria.</summary>
    public int IdBillete { get; init; }

    /// <summary>Fracción/vigésimo dentro del billete (0-19).</summary>
    public int Fraccion { get; init; }
}
