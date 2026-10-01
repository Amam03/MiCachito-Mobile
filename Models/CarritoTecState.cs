namespace MiCachito.Mobile.Models;

/// <summary>
/// Carrito de venta TEC persistido en SecureStorage.
///
/// Guarda lo mínimo necesario para REPINTAR el carrito tras reiniciar la
/// app: qué boleto eligió el billetero (id real para la venta) y con qué
/// datos se muestra (número, sorteo, precio, color).
///
/// Deliberadamente NO guarda estado de venta: eso vive en
/// billetes_loteria, en el backend. Si otro canal vendió el boleto mientras
/// la app estaba cerrada, la venta se rechaza con 409 al confirmar, en vez
/// de mostrar un total que ya no existe.
/// </summary>
public sealed class CarritoTecState
{
    /// <summary>
    /// A qué billetero pertenece este carrito. Evita que al cambiar de
    /// usuario en el mismo dispositivo un billetero vea los boletos del
    /// anterior (o intente venderlos: el backend los rechaza con 403, pero
    /// mejor no mostrar lo ajeno).
    /// </summary>
    public int IdBilletero { get; set; }

    /// <summary>Boletos elegidos (orden de selección).</summary>
    public List<CarritoTecItem> Billetes { get; set; } = new();
}

/// <summary>Un boleto dentro del carrito persistido.</summary>
public sealed class CarritoTecItem
{
    /// <summary>id_billete real (el que se envía a la venta).</summary>
    public int IdBillete { get; set; }

    /// <summary>Sorteo al que pertenece.</summary>
    public int IdSorteo { get; set; }

    /// <summary>Número impreso del boleto (para mostrarlo).</summary>
    public string Numero { get; set; } = string.Empty;

    /// <summary>Nombre del sorteo (para el carrito).</summary>
    public string NombreSorteo { get; set; } = string.Empty;

    /// <summary>
    /// Precio del boleto COMPLETO tal como llegó de la API. Es solo para
    /// mostrar: el total definitivo lo calcula el backend al vender.
    /// </summary>
    public decimal Precio { get; set; }

    /// <summary>Color de la tarjeta del sorteo.</summary>
    public string ColorHex { get; set; } = string.Empty;
}