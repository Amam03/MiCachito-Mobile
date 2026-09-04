using CommunityToolkit.Mvvm.ComponentModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Tienda / disponibilidad de boletos mostrada en la pantalla
/// "Agregar Boletos" (pantallas 9.1 / 9.2), que aparece tras seleccionar
/// la ciudad (pantalla 8) dentro del flujo de venta LOTENAL.
///
/// Cada fila del mockup combina datos de billete (número, fracciones y,
/// para zodiaco, el signo) con la ciudad/estado de la tienda donde está
/// disponible, más un botón de carrito por fila.
///
/// ESTADO MUTABLE (pantallas 10.x / 11): Seleccionadas y TotalDisponible
/// cambian al aceptar cantidades / eliminar / vender, y notifican a la
/// UI (la fila muestra "{seleccionadas}/{disponibles}" en vivo).
///
/// Mapeo backend (futuro): la fila mezcla conceptos de dos entidades:
///   - tiendas_sucursales: ciudad, estado, numero_sucursal
///   - billetes_loteria: numero_billete, signo_nombre, fracciones
/// La integración deberá definir el endpoint exacto que alimenta esta
/// pantalla (ver NOTAS_NEGOCIO_SORTEOS.md, punto abierto).
/// </summary>
public class TiendaDisponible : ObservableObject
{
    /// <summary>
    /// Id de la tienda/disponibilidad (mock). En el backend corresponderá
    /// a tiendas_sucursales.id_tienda_sucursal.
    /// </summary>
    public int IdTienda { get; init; }

    /// <summary>
    /// Número de billete tal como se muestra en el mockup
    /// (sin relleno de ceros, ej. "2168", "739").
    /// </summary>
    public string Numero { get; init; } = string.Empty;

    /// <summary>
    /// Texto de fracciones tal como aparece en el mockup (ej. "0/20").
    /// Fuente de datos original: no muta.
    /// </summary>
    public string FraccionesTexto { get; init; } = string.Empty;

    /// <summary>
    /// Signo zodiacal en MAYÚSCULAS (ej. "ARIES") para sorteos zodiaco
    /// (tipos 3 y 4). Null para sorteos normales.
    /// </summary>
    public string? Signo { get; init; }

    /// <summary>
    /// Ciudad en MAYÚSCULAS (ej. "TUXTLA GUTIERREZ").
    /// </summary>
    public string Ciudad { get; init; } = string.Empty;

    /// <summary>
    /// Estado en MAYÚSCULAS (ej. "CHIAPAS").
    /// </summary>
    public string Estado { get; init; } = string.Empty;

    /// <summary>
    /// Id de la ciudad/CEDIS (ver CiudadesCedisData de la pantalla 8).
    /// 0 = no mapeada al catálogo de la pantalla 8 (ciudades que solo
    /// aparecen en los mockups 9.x); solo se muestran en
    /// "Cualquier ciudad".
    /// </summary>
    public int IdCiudad { get; init; }

    /// <summary>
    /// Total de fracciones del billete, derivado de FraccionesTexto
    /// (ej. "0/20" → 20). Fuente inmutable: no se descuenta al vender.
    /// </summary>
    public int FraccionesTotal
    {
        get
        {
            var partes = FraccionesTexto.Split('/');
            return partes.Length > 1 && int.TryParse(partes[^1], out var total)
                ? total
                : 0;
        }
    }

    // ============ Estado mutable (pantallas 10.x / 11) ============

    private int _seleccionadas;

    /// <summary>
    /// Cachitos seleccionados para esta tienda en el carrito: se fija al
    /// pulsar Aceptar en el diálogo 10.x (la fila pasa de "0/20" a
    /// "n/20") y se limpia al Eliminar del carrito o Vender.
    /// 0 = sin selección.
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

    private bool _totalInicializado;
    private int _totalDisponible;

    /// <summary>
    /// Total de fracciones DISPONIBLES vigente: se inicializa una sola
    /// vez desde FraccionesTotal (ej. "0/20" → 20) y DISMINUYE al Vender
    /// (los boletos se descuentan del origen, pantalla 11). Al re-entrar
    /// a la pantalla 9.x se restaura al valor del catálogo.
    /// </summary>
    public int TotalDisponible
    {
        get
        {
            if (!_totalInicializado)
            {
                _totalDisponible = FraccionesTotal;
                _totalInicializado = true;
            }

            return _totalDisponible;
        }
        set
        {
            if (!_totalInicializado)
            {
                _totalDisponible = FraccionesTotal;
                _totalInicializado = true;
            }

            if (SetProperty(ref _totalDisponible, value))
            {
                OnPropertyChanged(nameof(FraccionesDisplay));
            }
        }
    }

    /// <summary>
    /// Texto de la línea 1 de la fila 9.x: "{seleccionadas}/{disponibles}".
    /// Inicia "0/20", refleja la selección al Aceptar ("1/20") y las
    /// ventas al Vender (el total baja). Notificado al cambiar cualquiera
    /// de los dos valores.
    /// </summary>
    public string FraccionesDisplay => $"{Seleccionadas}/{TotalDisponible}";

    /// <summary>
    /// True cuando la fila lleva línea de signo (sorteo zodiaco).
    /// </summary>
    public bool MostrarSigno => !string.IsNullOrEmpty(Signo);

    /// <summary>
    /// Texto de la línea de ciudad: "CIUDAD, ESTADO".
    /// </summary>
    public string DisplayName => $"{Ciudad}, {Estado}";
}
