namespace MiCachito.Mobile.Models.Requests;

/// <summary>
/// Datos del cliente que compra el boleto (pantalla 15 de Sorteos Tec).
///
/// Antes se escribian en la pantalla y se DESCARTABAN al confirmar la venta:
/// la peticion solo llevaba los boletos, la venta quedaba sin id_cliente y la
/// columna Cliente del reporte de escritorio salia vacia.
///
/// Se manda ahora dentro de la misma llamada que los boletos. El backend crea
/// un cliente nuevo por venta y guarda su id en la cabecera, para que el
/// reporte muestre una fila por transaccion con su comprador.
/// </summary>
public class ClienteVenta
{
    /// <summary>Nombre(s). Es el unico campo obligatorio: sin el, no hay cliente.</summary>
    public string Nombre { get; set; } = string.Empty;

    public string ApellidoPaterno { get; set; } = string.Empty;

    public string ApellidoMaterno { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    /// <summary>
    /// True si hay con que armar un cliente. Sin nombre, el backend registra la
    /// venta sin cliente en vez de crear un registro vacio.
    /// </summary>
    public bool EsUtilizable => !string.IsNullOrWhiteSpace(Nombre);
}