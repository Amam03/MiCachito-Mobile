namespace MiCachito.Mobile.Services;

/// <summary>
/// Contrato del flujo de impresión de tickets (mockup 5.1: botón
/// Imprimir). Fase SOLO INTERFAZ: prepara el flujo Bluetooth y avisa
/// cuando no hay impresora vinculada; la lógica real de impresión
/// (emparejar, conexión SPP, ESC/POS) queda para la integración
/// (ver docs/NOTAS_TICKETS_VENTA.md).
/// </summary>
public interface IImpresoraService
{
    /// <summary>
    /// Envía a imprimir el archivo indicado. Devuelve el mensaje de
    /// resultado/aviso para mostrar al usuario (ej. impresora no
    /// vinculada).
    /// </summary>
    Task<string> ImprimirAsync(string rutaArchivo, string titulo);

    /// <summary>
    /// Nombres de los dispositivos Bluetooth VINCULADOS al teléfono
    /// (mockup Cuenta 5: Dispositivos Enlazados). Lista vacía si el
    /// Bluetooth está apagado, falta el permiso o no hay vínculos
    /// (caso emulador). El emparejamiento real (discovery + SPP) queda
    /// para la integración.
    /// </summary>
    Task<IReadOnlyList<string>> ObtenerDispositivosEnlazadosAsync();
}
