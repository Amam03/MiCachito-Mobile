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
/// Mapeo backend (futuro): la fila mezcla conceptos de dos entidades:
///   - tiendas_sucursales: ciudad, estado, numero_sucursal
///   - billetes_loteria: numero_billete, signo_nombre, fracciones
/// La integración deberá definir el endpoint exacto que alimenta esta
/// pantalla (ver NOTAS_NEGOCIO_SORTEOS.md, punto abierto).
/// </summary>
public class TiendaDisponible
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
    /// True cuando la fila lleva línea de signo (sorteo zodiaco).
    /// </summary>
    public bool MostrarSigno => !string.IsNullOrEmpty(Signo);

    /// <summary>
    /// Texto de la línea de ciudad: "CIUDAD, ESTADO".
    /// </summary>
    public string DisplayName => $"{Ciudad}, {Estado}";
}
