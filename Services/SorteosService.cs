using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Servicio de la pantalla Sorteos (mockups 4, 4.1, 4.2). Fase SOLO
/// INTERFAZ: el listado de sorteos sale del catálogo del calendario
/// LOTENAL ya revisado (SorteosActivosLotenalData) filtrado a los
/// CELEBRADOS (fecha &lt;= hoy) y ordenado descendente por fecha.
///
/// Decisión del usuario (2026-09-08): TODAS las cifras se muestran en
/// $0.00 — el flujo visual (tarjetas de folio, tabla de pagos, toggles)
/// ya fue verificado con cifras sintéticas y aprobado; los cálculos
/// reales llegarán con la integración al backend (ver
/// docs/NOTAS_SORTEOS.md). Sin folios ni pagos de ejemplo: las listas
/// de detalle van vacías hasta que el backend las llene.
/// </summary>
public class SorteosService
{
    /// <summary>
    /// Sorteos celebrados de TODOS los tipos del catálogo (Mayor, Superior,
    /// Zodiaco, Zodiaco Especial, Especial, Gran Especial, Magno, Gordito),
    /// ordenados por fecha de celebración DESCENDENTE (más reciente arriba).
    /// </summary>
    public IReadOnlyList<SorteoActivoLotenal> SorteosCelebrados()
    {
        DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
        return SorteosActivosLotenalData.ObtenerTodos()
            .Where(s => s.FechaCelebracion <= hoy)
            .OrderByDescending(s => s.FechaCelebracion)
            .ToList();
    }

    /// <summary>
    /// Información del sorteo con TODAS las cifras en cero y listas de
    /// detalle vacías (decisión del usuario; el backend calculará).
    /// </summary>
    public SorteoCelebradoInfo ObtenerInfo(SorteoActivoLotenal sorteo)
    {
        return new SorteoCelebradoInfo
        {
            Sorteo = sorteo,
            Ventas = 0m,
            PagosRealizados = 0m,
            Entregas = [],
            TotalDevolucion = 0m,
            Pagos = [],
        };
    }
}
