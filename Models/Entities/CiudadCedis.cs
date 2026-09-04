namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Ciudad / CEDIS disponible para la venta de un sorteo LOTENAL.
/// Se muestra en la pantalla "Seleccionar Ciudad" (pantalla 8), que aparece
/// tras seleccionar un sorteo activo en la pantalla 7.x.
///
/// Mapeo backend (futuro): GET /api/cedis (CedisController::actionIndex)
/// devuelve la lista de CEDIS con sede-scoping:
///   - tipo corporativo: todos los CEDIS
///   - usuario con id_cedis: solo su CEDIS
///
/// NOTA de desincronización detectada: el modelo backend Cedis.php declara
/// los atributos ciudad/estado, pero la tabla real `cedis` NO tiene esas
/// columnas (tiene codigo_org_ln/numero_tienda_ln); la ciudad va embebida
/// en nombre_cedis (ej. "CEDIS Puebla"). La integración deberá resolver
/// esta desincronización antes de consumir el endpoint.
/// </summary>
public class CiudadCedis
{
    /// <summary>
    /// Id de la ciudad/CEDIS. El Id 0 está reservado para la opción
    /// "CUALQUIER CIUDAD". En el backend corresponde a cedis.id_cedis.
    /// </summary>
    public int IdCiudad { get; init; }

    /// <summary>
    /// Nombre de la ciudad en MAYÚSCULAS (ej. "CARDENAS").
    /// </summary>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>
    /// Estado en MAYÚSCULAS (ej. "TABASCO"). Null para "Cualquier ciudad".
    /// </summary>
    public string? Estado { get; init; }

    /// <summary>
    /// True para la opción especial "CUALQUIER CIUDAD" (IdCiudad == 0).
    /// </summary>
    public bool EsCualquierCiudad => IdCiudad == 0;

    /// <summary>
    /// Texto del botón: "CUALQUIER CIUDAD" o "CIUDAD, ESTADO"
    /// (formato del mockup, centrado en el botón).
    /// </summary>
    public string DisplayName =>
        EsCualquierCiudad ? Nombre : $"{Nombre}, {Estado}";
}
