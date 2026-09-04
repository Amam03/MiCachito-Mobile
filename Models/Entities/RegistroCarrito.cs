namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Registro del carrito de compras (pantalla 11): los cachitos aceptados
/// de UNA tienda para el sorteo en curso.
///
/// Todo se DERIVA de la fila del catálogo (TiendaDisponible) y del sorteo
/// activo (SorteoActivoLotenal): no hay datos hardcoded.
///   - SorteoTexto: "{nombre corto del sorteo} {NúmeroSorteo}"
///     (ej. "SUPERIOR 2894", "GRAN ESPECIAL 208").
///   - Precio: precio individual por CACHITO = precio del sorteo de la
///     fuente existente (SorteosActivosLotenalData, alineada al backend:
///     MAYOR $30, SUPERIOR $40, ZODIACO $20, ZODIACO ESPECIAL $35,
///     ESPECIAL $60, GRAN ESPECIAL $250, MAGNO $120, GORDITO NAVIDEÑO
///     $120). No se duplica ni inventa otra fuente.
///   - Total: cantidad × precio.
/// </summary>
public class RegistroCarrito
{
    /// <summary>
    /// Tienda de origen (IdTienda de TiendaDisponible): al Eliminar/Vender
    /// se localiza la fila del catálogo por este id.
    /// </summary>
    public int IdTienda { get; init; }

    /// <summary>Sorteo + número (ej. "SUPERIOR 2894").</summary>
    public string SorteoTexto { get; init; } = string.Empty;

    /// <summary>Ciudad, estado de la tienda de origen.</summary>
    public string TiendaTexto { get; init; } = string.Empty;

    /// <summary>Cachitos seleccionados en esa tienda.</summary>
    public int Cantidad { get; init; }

    /// <summary>Precio individual por cachito (derivado del sorteo).</summary>
    public decimal Precio { get; init; }

    /// <summary>Importe total del registro (cantidad × precio).</summary>
    public decimal Total => Cantidad * Precio;

    /// <summary>Color del sorteo (círculo de la tarjeta).</summary>
    public string ColorHex { get; init; } = string.Empty;

    /// <summary>Precio individual formateado (ej. "Precio individual: $2.00").</summary>
    public string PrecioIndividualTexto => $"Precio individual: ${Precio:0.00}";

    /// <summary>Cantidad formateada (ej. "Cantidad: 20 boletos").</summary>
    public string CantidadTexto => Cantidad == 1
        ? "Cantidad: 1 boleto"
        : $"Cantidad: {Cantidad} boletos";

    /// <summary>Total formateado (ej. "Total: $40.00").</summary>
    public string TotalTexto => $"Total: ${Total:0.00}";
}
