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

    /// <summary>
    /// Dispositivos Bluetooth VINCULADOS con dirección + nombre (F6):
    /// candidatos a impresora térmica. El emparejamiento se hace desde
    /// Ajustes de Android (flujo del SO); la app solo LISTA los
    /// vinculados y conecta vía SPP.
    /// </summary>
    Task<IReadOnlyList<DispositivoBluetooth>> ObtenerDispositivosAsync();

    /// <summary>
    /// Fija la impresora seleccionada (dirección MAC) en Preferences:
    /// se conserva entre sesiones para no volver a seleccionarla en
    /// cada operación (F6).
    /// </summary>
    void SeleccionarImpresora(string direccionMac);

    /// <summary>
    /// Dirección MAC de la impresora seleccionada persistida; null si
    /// nunca se ha seleccionado una o se deseleccionó.
    /// </summary>
    string? ImpresoraSeleccionada();

    /// <summary>
    /// IMPRIME un comprobante LOTENAL (F6): texto ESC/POS vía Bluetooth
    /// SPP (UUID 00001101-0000-1000-8000-00805F9B34FB) a la impresora
    /// SELECCIONADA (Preferences) o a la única vinculada si no hay
    /// selección. NUNCA repite la operación de negocio: el comprobante
    /// ya existe; solo se reenvía el texto a la impresora.
    /// Devuelve el resultado para la UI (éxito o error controlado).
    /// </summary>
    Task<ResultadoImpresion> ImprimirComprobanteAsync(ComprobanteLotenal comprobante);
}

/// <summary>
/// Dispositivo Bluetooth vinculado con dirección MAC (F6). La dirección
/// es el identificador estable del vínculo (persistible); el nombre es
/// solo presentación.
/// </summary>
public sealed class DispositivoBluetooth
{
    /// <summary>Dirección MAC ("AA:BB:CC:DD:EE:FF") — identificador estable.</summary>
    public required string Direccion { get; init; }

    /// <summary>Nombre del dispositivo (presentación; puede ser null).</summary>
    public string? Nombre { get; init; }

    public string DisplayName => string.IsNullOrWhiteSpace(Nombre)
        ? Direccion
        : Nombre!;
}

/// <summary>Resultado controlado de la impresión (F6): nunca lanza.</summary>
public sealed class ResultadoImpresion
{
    /// <summary>True cuando el comprobante se envió completo a la impresora.</summary>
    public required bool Exito { get; init; }

    /// <summary>Mensaje para el usuario (éxito o error con causa).</summary>
    public required string Mensaje { get; init; }

    /// <summary>True cuando el fallo se puede corregir seleccionando otra impresora.</summary>
    public bool RequiereSeleccion { get; init; }

    /// <summary>Éxito con mensaje.</summary>
    public static ResultadoImpresion Ok(string mensaje) => new() { Exito = true, Mensaje = mensaje };

    /// <summary>Fallo controlado; RequiereSeleccion cuando no hay/desconectada.</summary>
    public static ResultadoImpresion Fallo(string mensaje, bool requiereSeleccion = false) =>
        new() { Exito = false, Mensaje = mensaje, RequiereSeleccion = requiereSeleccion };
}

/// <summary>
/// Comprobante LOTENAL imprimible (F6): venta o devolución con los
/// datos REALES de la operación. El texto ESC/POS lo arma el servicio;
/// la UI solo provee estos campos.
/// </summary>
public sealed class ComprobanteLotenal
{
    /// <summary>Tipo de comprobante (encabezado del ticket).</summary>
    public required string Tipo { get; init; }

    /// <summary>Folio real (venta "V-..." o movimiento "MA-...").</summary>
    public required string Folio { get; init; }

    /// <summary>Fecha/hora de la operación (texto, ya formateado).</summary>
    public required string FechaHora { get; init; }

    /// <summary>Nombre del billetero (sesión mobile).</summary>
    public required string Billetero { get; init; }

    /// <summary>Producto + dotación (ej. "MAYOR No. 4024").</summary>
    public required string ProductoSorteo { get; init; }

    /// <summary>Líneas del desglose (número/serie/fracciones/precio o cantidad).</summary>
    public IReadOnlyList<string> Lineas { get; init; } = [];

    /// <summary>Total correspondiente (texto formateado, ej. "$140.00").</summary>
    public required string Total { get; init; }
}
