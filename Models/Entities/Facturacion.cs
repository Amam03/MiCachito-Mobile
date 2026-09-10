using System.Collections.ObjectModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Reporte de Facturación (pestaña 3 de Reportes, mockups 9.3).
/// Contiene los registros FILTRADOS por el periodo elegido y la
/// agrupación por categoría con porcentajes calculados. Los totales
/// son dinámicos (derivados de los registros) — nunca fijos.
/// </summary>
public class Facturacion
{
    /// <summary>Inicio del periodo seleccionado por el usuario.</summary>
    public DateTime FechaInicio { get; set; }

    /// <summary>Fin del periodo seleccionado por el usuario.</summary>
    public DateTime FechaFin { get; set; }

    /// <summary>Nombre del vendedor/agente (vendrá de la sesión real).</summary>
    public string Vendedor { get; set; } = string.Empty;

    /// <summary>Registros del periodo (filtrados de la fuente local).</summary>
    public ObservableCollection<RegistroFacturacion> Registros { get; } = new();

    /// <summary>Agrupación por categoría con montos y porcentajes calculados.</summary>
    public ObservableCollection<CategoriaFacturacion> Categorias { get; } = new();

    /// <summary>Total facturado del periodo (columna Venta).</summary>
    public decimal TotalFacturado => Registros.Sum(r => r.Venta);

    /// <summary>Subtotal de una categoría (columna Venta).</summary>
    public decimal Subtotal(string categoria) =>
        Registros.Where(r => r.Sorteo == categoria).Sum(r => r.Venta);
}
