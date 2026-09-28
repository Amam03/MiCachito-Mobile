using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

/// <summary>
/// Estado en memoria de la venta LOTENAL en curso (F2): la dotación
/// activa y sus series cargadas de la API, compartidas entre la
/// pantalla 9.x (lista/overlay 10.x) y el carrito (11). Mismo papel
/// que los catálogos estáticos del mock (TiendasDisponiblesData),
/// pero alimentado por GET api/mobile/ventas/billetes.
///
/// NO es un inventario paralelo: billetes_loteria vía backend sigue
/// siendo la fuente de verdad del estado LIVE — al entrar a una
/// dotación la app RECONSULTA y este estado se reemplaza completo
/// (selecciones y descuentos locales no sobreviven la reentrada).
/// </summary>
public static class SesionVentaLotenal
{
    /// <summary>Dotación activa (null = sin flujo en curso).</summary>
    public static DotacionDisponible? Dotacion { get; private set; }

    /// <summary>
    /// Series de la dotación activa (mismas instancias que la lista
    /// 9.x: mutar Seleccionadas/TotalDisponible desde el carrito se
    /// refleja en la lista sin recargar).
    /// </summary>
    public static IReadOnlyList<SerieDisponible> Series { get; private set; } = [];

    /// <summary>
    /// Fija la dotación activa y sus series (al cargar la pantalla
    /// 9.x; reemplaza cualquier estado anterior).
    /// </summary>
    public static void Establecer(DotacionDisponible dotacion, IReadOnlyList<SerieDisponible> series)
    {
        Dotacion = dotacion;
        Series = series;
    }

    /// <summary>Limpia el estado de la venta en curso (F3 tras vender).</summary>
    public static void Limpiar()
    {
        Dotacion = null;
        Series = [];
    }
}
