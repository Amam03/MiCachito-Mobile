using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Responses;

/// <summary>
/// Fila del historial de devoluciones (Gestión > Devolución 6.x):
/// GET api/mobile/devoluciones. Movimientos_almacen tipo 'devolucion'
/// del billetero de la sesión (registrados desde Mobile o Escritorio),
/// fecha descendente. La lista reconstruye cada devolución guardada y
/// sus cachitos; el detalle por sorteo sigue en el flujo de captura.
/// </summary>
public sealed class DevolucionHistorialApi
{
    /// <summary>PK del movimiento (movimientos_almacen.id_movimiento).</summary>
    [JsonPropertyName("id_movimiento")]
    public int IdMovimiento { get; set; }

    /// <summary>Folio del movimiento ("MA-20260928-0002").</summary>
    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>Estatus administrativo ("pendiente" hasta liquidar en Desktop).</summary>
    [JsonPropertyName("estatus")]
    public string Estatus { get; set; } = string.Empty;

    /// <summary>Id del sorteo de la devolución.</summary>
    [JsonPropertyName("id_sorteo")]
    public int IdSorteo { get; set; }

    /// <summary>Label "NOMBRE - DOTACION" (o solo NOMBRE si no hay dotación).</summary>
    [JsonPropertyName("sorteo")]
    public string? Sorteo { get; set; }

    /// <summary>Nombre del producto LN sin prefijo, MAYÚSCULAS ("MAYOR").</summary>
    [JsonPropertyName("nombre_producto")]
    public string? NombreProducto { get; set; }

    /// <summary>Dotación (billetes_loteria.numero_sorteo); null si no aplica.</summary>
    [JsonPropertyName("dotacion")]
    public string? Dotacion { get; set; }

    /// <summary>Fecha de celebración del sorteo ("yyyy-MM-dd").</summary>
    [JsonPropertyName("fecha_sorteo")]
    public string? FechaSorteo { get; set; }

    /// <summary>Fecha del movimiento ("yyyy-MM-dd").</summary>
    [JsonPropertyName("fecha_movimiento")]
    public string? FechaMovimiento { get; set; }

    /// <summary>Cachitos devueltos (filas del detalle del movimiento).</summary>
    [JsonPropertyName("total_cachitos")]
    public int TotalCachitos { get; set; }

    /// <summary>
    /// Total en dinero del movimiento (precio × cachitos, calculado por
    /// el backend con el precio REAL del sorteo). Llega como número
    /// JSON (mismo patrón que DepositoApi.Monto).
    /// </summary>
    [JsonPropertyName("total_monto")]
    public decimal TotalMonto { get; set; }
}
