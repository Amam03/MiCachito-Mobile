using MiCachito.Mobile.Api;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Fuente del reporte de Facturación (pestaña 3 de Reportes, mockup
/// 9.3) contra la API real: GET api/mobile/reportes/facturacion con el
/// rango del modal "Seleccionar Fechas". Todo queda acotado al billetero
/// de la sesión (token); el endpoint no acepta ids del cliente.
///
/// El backend ya entrega venta y ganancia calculadas (ganancia = venta ×
/// comisión real del billetero); este servicio SOLO mapea y agrupa por
/// categoría para el pie/leyenda (monto y % de la columna Venta) — no
/// duplica lógica de negocio. Lanza ApiException en errores HTTP; la red
/// caída llega como HttpRequestException/TaskCanceled — el VM la captura
/// y muestra "sin conexión" sin inventar datos.
/// </summary>
public class FacturacionService
{
    private const string Ruta = "api/mobile/reportes/facturacion";

    /// <summary>
    /// Colores de presentación por categoría (mockup 9.3 — estilo, no
    /// dato). Las categorías fuera del mockup usan el gris de fallback
    /// preexistente; no se inventan colores nuevos.
    /// </summary>
    private static readonly Dictionary<string, string> Colores = new()
    {
        ["Mayor"] = "#FFB600",
        ["Superior"] = "#4D9D2E",
        ["Zodiaco"] = "#ED40A9",
        ["Especial"] = "#0278D7",
    };

    private readonly IApiClient _api;

    public FacturacionService(IApiClient api)
    {
        _api = api;
    }

    /// <summary>
    /// Consulta la facturación del periodo y construye el reporte:
    /// registros en orden cronológico (fecha|sorteo) + agrupación por
    /// categoría con monto (columna Venta) y porcentaje calculado.
    /// </summary>
    public async Task<Facturacion?> ObtenerAsync(DateTime inicio, DateTime fin, CancellationToken cancellationToken = default)
    {
        string path = $"{Ruta}?fecha_inicio={inicio:yyyy-MM-dd}&fecha_fin={fin:yyyy-MM-dd}";
        FacturacionApi? api = await _api.GetAsync<FacturacionApi>(path, cancellationToken)
            .ConfigureAwait(false);
        if (api is null)
        {
            return null;
        }

        var reporte = new Facturacion
        {
            FechaInicio = inicio,
            FechaFin = fin,
            Vendedor = api.Vendedor,
            ComisionPct = api.ComisionPct,
        };

        foreach (RegistroFacturacionApi r in api.Registros ?? new List<RegistroFacturacionApi>())
        {
            // Fecha cruda del backend → DateOnly es-MX seguro (patrón FA/Recibos).
            if (!FormatosFecha.TryParseFechaBackend(r.Fecha, out DateOnly fecha))
            {
                continue; // sin fecha no hay fila confiable — no inventar
            }

            reporte.Registros.Add(new RegistroFacturacion
            {
                Fecha = fecha.ToDateTime(TimeOnly.MinValue),
                Sorteo = r.Sorteo,
                // Sin categoría (producto faltante) → "N/A", convención del
                // propio backend para etiquetas ausentes — no se inventa nombre
                // y registro/agrupación/PDF quedan consistentes.
                Categoria = string.IsNullOrWhiteSpace(r.Categoria) ? "N/A" : r.Categoria,
                Entrega = r.Entrega,
                Devolucion = r.Devolucion,
                Venta = r.Venta,
                Ganancia = r.Ganancia,
            });
        }

        // Pie/leyenda: agrupación por CATEGORÍA (producto); monto = Venta.
        decimal total = reporte.TotalFacturado;
        foreach (IGrouping<string, RegistroFacturacion> grupo in reporte.Registros.GroupBy(r => r.Categoria))
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

        return reporte;
    }
}
