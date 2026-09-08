namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Tipo de venta registrada en Tickets de Venta (pestaña Gestión,
/// mockups 5, 5.1, 5.2). Cada tipo viene de un módulo distinto de
/// Vender y tiene campos de detalle propios.
/// </summary>
public enum TipoVenta
{
    /// <summary>Venta de billete/fracciones LOTENAL (módulo Lotenal).</summary>
    Lotenal,

    /// <summary>Venta de billete de Sorteos Tec (módulo Sorteos Tec).</summary>
    SorteosTec,

    /// <summary>Recarga/paquete de Tiempo Aire (módulo Tiempo Aire).</summary>
    TiempoAire,
}

/// <summary>
/// Un movimiento de venta del listado "Ventas" (mockup 5): registro
/// temporal EN MEMORIA creado por TicketsVentaService para probar la
/// interfaz y el flujo (listado, detalle, PDF, compartir, impresión).
/// NO es un seed ni mock permanente — la fuente real será el backend
/// (ver docs/NOTAS_TICKETS_VENTA.md).
/// </summary>
public class MovimientoVenta
{
    public int Folio { get; init; }

    public TipoVenta Tipo { get; init; }

    /// <summary>Fecha del movimiento (mockup: "01-septiembre-2023").</summary>
    public DateOnly Fecha { get; init; }

    /// <summary>Nombre del cliente (mockup: "MAURICIO LOPEZ GONZALEZ").</summary>
    public string Cliente { get; init; } = string.Empty;

    /// <summary>Importe total de la venta.</summary>
    public decimal Importe { get; init; }

    // ── Descripción del producto por tipo (fila del listado) ──────────

    /// <summary>
    /// Línea principal de la fila: producto/sorteo (Lotenal/Tec) o
    /// "Tiempo Aire Electrónico" (TA).
    /// </summary>
    public string Descripcion { get; init; } = string.Empty;

    // ── LOTENAL / Sorteos Tec ─────────────────────────────────────────

    /// <summary>Nombre del sorteo (ej. "SORTEO MAYOR 4024", "AVENTURA TEC 27").</summary>
    public string? Sorteo { get; init; }

    /// <summary>Número de billete/boleto vendido.</summary>
    public string? Boleto { get; init; }

    /// <summary>Valor unitario del billete.</summary>
    public decimal? Valor { get; init; }

    // ── Tiempo Aire ───────────────────────────────────────────────────

    /// <summary>Compañía del proveedor (ej. "Telcel").</summary>
    public string? Compania { get; init; }

    /// <summary>Teléfono recargado.</summary>
    public string? Telefono { get; init; }

    /// <summary>Monto o paquete de la recarga.</summary>
    public string? MontoPaquete { get; init; }

    // ── PDF (mockup 5.2): datos del comprobante ───────────────────────

    /// <summary>Plaza (ej. "PUEBLA").</summary>
    public string Plaza { get; init; } = string.Empty;

    /// <summary>CEDIS emisor (ej. "CEDIS PUEBLA").</summary>
    public string Cedis { get; init; } = string.Empty;

    /// <summary>Vendedor que registró la venta.</summary>
    public string Vendedor { get; init; } = string.Empty;

    /// <summary>Datos del cliente para el PDF: nombre.</summary>
    public string? ClienteNombrePdf { get; init; }

    /// <summary>Datos del cliente para el PDF: teléfono.</summary>
    public string? ClienteTelefonoPdf { get; init; }

    /// <summary>Datos del cliente para el PDF: correo.</summary>
    public string? ClienteCorreoPdf { get; init; }

    /// <summary>
    /// Folio de compra de Sorteos Tec (solo tipo SorteosTec; mockup:
    /// "Folio de Compra de Sorteos Tec: 7448622").
    /// </summary>
    public string? FolioCompraTec { get; init; }

    /// <summary>
    /// Fecha y hora exacta del movimiento para el pie del PDF
    /// (mockup: "2026-09-04 11:12:51.320").
    /// </summary>
    public DateTime FechaHoraMovimiento { get; init; }

    /// <summary>Color del chip identificador por tipo (lista, mockup 5).</summary>
    public string ColorChipHex
    {
        get
        {
            return Tipo switch
            {
                TipoVenta.Lotenal => "#FF0080",   // McLotenal
                TipoVenta.SorteosTec => "#1A3688", // McSorteosTec
                _ => "#2879FE",                    // McTiempoAire
            };
        }
    }

    /// <summary>Importe formateado para la fila del listado.</summary>
    public string ImporteText => $"${Importe:0.00}";

    /// <summary>Fecha corta de la fila (mockup: "01-sep-2023").</summary>
    public string FechaText => Fecha.ToString("dd-MMM-yyyy", new System.Globalization.CultureInfo("es-MX"));

    /// <summary>Folio + fecha de la fila (mockup: "Folio: 5369  01-sep-2023").</summary>
    public string FolioFechaText => $"Folio: {Folio}   {FechaText}";

    /// <summary>
    /// Nombre base del archivo PDF: ticket-{tipo}-{folio} (mockup 5.2:
    /// "ticket-st-5369-0...").
    /// </summary>
    public string NombreArchivoPdf =>
        $"ticket-{(Tipo == TipoVenta.SorteosTec ? "st" : Tipo == TipoVenta.Lotenal ? "lot" : "ta")}-{Folio}";
}
