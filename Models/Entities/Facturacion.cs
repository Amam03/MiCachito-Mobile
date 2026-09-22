using System.Collections.ObjectModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Reporte de Facturación (pestaña 3 de Reportes, mockups 9.3) construido
/// con los datos reales de GET api/mobile/reportes/facturacion: registros
/// del periodo (una fila por fecha|sorteo, en dinero) y agrupación por
/// CATEGORÍA con porcentajes calculados. Los totales son dinámicos
/// (derivados de los registros) — nunca fijos.
/// </summary>
public class Facturacion
{
    /// <summary>Inicio del periodo seleccionado por el usuario.</summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>Fin del periodo seleccionado por el usuario.</summary>
    public DateTime FechaFin { get; set; }

    /// <summary>Nombre del vendedor (billetero de la sesión, del endpoint).</summary>
    public string Vendedor { get; set; } = string.Empty;

    /// <summary>Porcentaje de comisión del billetero (del endpoint; la ganancia ya viene calculada).</summary>
    public decimal ComisionPct { get; set; }

    /// <summary>Registros del periodo (una fila por fecha|sorteo, del backend).</summary>
    public ObservableCollection<RegistroFacturacion> Registros { get; } = new();

    /// <summary>Agrupación por categoría con montos y porcentajes calculados.</summary>
    public ObservableCollection<CategoriaFacturacion> Categorias { get; } = new();

    /// <summary>Total facturado del periodo (columna Venta).</summary>
    public decimal TotalFacturado => Registros.Sum(r => r.Venta);

    /// <summary>Subtotal de una categoría (columna Venta).</summary>
    public decimal Subtotal(string categoria) =>
        Registros.Where(r => r.Categoria == categoria).Sum(r => r.Venta);
}
