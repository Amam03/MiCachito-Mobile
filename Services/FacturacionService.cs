using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Fuente del reporte de Facturación (pestaña 3 de Reportes).
///
/// TEMPORAL para validación de UI/flujo (directriz del usuario, mockups
/// 9.3): mantiene en MEMORIA 2 registros de prueba con fechas desde el
/// 08/09/2026 para poder comprobar el pie, porcentajes, leyenda, detalle
/// y PDF con más de una categoría. NO son seeds, NO van a BD, NO son
/// mocks permanentes — al conectar el backend este servicio se sustituye
/// por la consulta real (docs/NOTAS_FACTURACION.md).
///
/// El filtro por rango es REAL: solo los registros cuya fecha cae dentro
/// del periodo aparecen en el reporte; totales, agrupación y porcentajes
/// se calculan dinámicamente a partir de los registros filtrados.
/// </summary>
public class FacturacionService
{
    /// <summary>Colores de presentación por categoría (mockup 9.3).</summary>
    private static readonly Dictionary<string, string> Colores = new()
    {
        ["Mayor"] = "#FFB600",
        ["Superior"] = "#4D9D2E",
        ["Zodiaco"] = "#ED40A9",
        ["Especial"] = "#0278D7",
    };

    /// <summary>
    /// Registros locales de prueba (solo en memoria, fechas ≥ 08/09/2026).
    /// Montos coherentes: Devolución = Entrega − Venta; Ganancia = 7.5% de la venta.
    /// </summary>
    private static readonly List<RegistroFacturacion> RegistrosPrueba = new()
    {
        new RegistroFacturacion
        {
            Fecha = new DateTime(2026, 9, 8),
            Sorteo = "Mayor",
            Entrega = 1500.00m,
            Devolucion = 264.50m,
            Venta = 1235.50m,
            Ganancia = 92.66m,
        },
        new RegistroFacturacion
        {
            Fecha = new DateTime(2026, 9, 9),
            Sorteo = "Superior",
            Entrega = 900.00m,
            Devolucion = 135.80m,
            Venta = 764.20m,
            Ganancia = 57.32m,
        },
    };

    /// <summary>
    /// Construye el reporte del periodo: filtra los registros por fecha
    /// (REAL) y agrupa por categoría con monto y porcentaje calculados.
    /// </summary>
    public Task<Facturacion> ObtenerAsync(DateTime inicio, DateTime fin)
    {
        var reporte = new Facturacion
        {
            FechaInicio = inicio,
            FechaFin = fin,
            Vendedor = string.Empty,
        };

        foreach (RegistroFacturacion r in RegistrosPrueba.Where(r => r.Fecha >= inicio.Date && r.Fecha <= fin.Date))
        {
            reporte.Registros.Add(r);
        }

        decimal total = reporte.TotalFacturado;
        foreach (IGrouping<string, RegistroFacturacion> grupo in reporte.Registros.GroupBy(r => r.Sorteo))
        {
            decimal monto = grupo.Sum(r => r.Venta);
            reporte.Categorias.Add(new CategoriaFacturacion
            {
                Nombre = grupo.Key,
                ColorHex = Colores.TryGetValue(grupo.Key, out string? color) ? color : "#9E9E9E",
                Monto = monto,
                Porcentaje = total > 0 ? (double)(monto / total) * 100.0 : 0,
            });
        }

        return Task.FromResult(reporte);
    }
}
