using System.Text.Json.Serialization;
using MiCachito.Mobile.Models.Responses;

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
/// Un movimiento de venta del listado "Ventas" (mockup 5): UNA venta real
/// del billetero de la sesión (tabla `ventas`), construida desde el
/// historial (GET api/mobile/ventas) o el detalle (GET api/mobile/ventas/{id}).
/// La clave de navegación es IdVenta (ventas.id_venta); Folio es SOLO
/// display ("1103" o "V-20260831-67707" legacy).
/// </summary>
public class MovimientoVenta
{
    /// <summary>PK de la venta (ventas.id_venta) — clave de navegación al detalle.</summary>
    public int IdVenta { get; init; }

    /// <summary>Folio de la venta ("1103"; legacy "V-20260831-67707"). Display.</summary>
    public string Folio { get; init; } = string.Empty;

    public TipoVenta Tipo { get; init; }

    /// <summary>Estatus de la venta: activa | pagada | cancelada | devuelta (D5).</summary>
    public string Estatus { get; init; } = "activa";

    /// <summary>Fecha del movimiento (día de la venta).</summary>
    public DateOnly Fecha { get; init; }

    /// <summary>
    /// Nombre del cliente capturado en la venta o "PÚBLICO EN GENERAL"
    /// cuando la venta no trae cliente (D4: la app nunca inventa datos).
    /// </summary>
    public string Cliente { get; init; } = "PÚBLICO EN GENERAL";

    /// <summary>Importe total de la venta.</summary>
    public decimal Importe { get; init; }

    // ── Descripción del producto por tipo (fila del listado) ──────────

    /// <summary>
    /// Línea principal de la fila: producto/sorteo (Lotenal/Tec) o
    /// "Tiempo Aire Electrónico" (TA).
    /// </summary>
    public string Descripcion { get; init; } = string.Empty;

    // ── LOTENAL / Sorteos Tec ─────────────────────────────────────────

    /// <summary>Nombre del sorteo (ej. "Sorteo 041753", "Sorteo Mi Sueño").</summary>
    public string? Sorteo { get; init; }

    /// <summary>
    /// Números de billete/boleto vendidos, unidos por coma ("0101, 0102"
    /// en ventas de varias partidas — D2) o null en Tiempo Aire.
    /// Partidas legacy sin trazabilidad → "—".
    /// </summary>
    public string? Boleto { get; init; }

    /// <summary>
    /// Valor unitario de la PRIMERA partida (mockup 5.1 muestra un valor).
    /// </summary>
    public decimal? Valor { get; init; }

    /// <summary>Partidas completas de la venta (solo con detalle cargado).</summary>
    public IReadOnlyList<PartidaVentaApi>? Partidas { get; init; }

    // ── Tiempo Aire ───────────────────────────────────────────────────

    /// <summary>Compañía del proveedor (ej. "Telcel").</summary>
    public string? Compania { get; init; }

    /// <summary>Teléfono recargado.</summary>
    public string? Telefono { get; init; }

    /// <summary>Monto o paquete de la recarga.</summary>
    public string? MontoPaquete { get; init; }

    // ── PDF (mockup 5.2): datos del comprobante ───────────────────────

    /// <summary>CEDIS emisor ("CEDIS Puebla") — D1: Plaza se omite (sin ciudad en BD).</summary>
    public string Cedis { get; init; } = string.Empty;

    /// <summary>Vendedor que registró la venta (= billetero de la sesión).</summary>
    public string Vendedor { get; init; } = string.Empty;

    /// <summary>Datos del cliente para el PDF: nombre.</summary>
    public string? ClienteNombrePdf { get; init; }

    /// <summary>Datos del cliente para el PDF: teléfono.</summary>
    public string? ClienteTelefonoPdf { get; init; }

    /// <summary>Datos del cliente para el PDF: correo.</summary>
    public string? ClienteCorreoPdf { get; init; }

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
    /// Estatus discreto junto al folio (D3): texto en gris cuando la
    /// venta NO está activa (cancelada/devuelta); vacío en activa/pagada
    /// para no ensuciar la fila del mockup.
    /// </summary>
    public string EstatusText =>
        Estatus is "cancelada" or "devuelta" ? Estatus.ToUpperInvariant() : string.Empty;

    /// <summary>
    /// Nombre base del archivo PDF: ticket-{tipo}-{folio} (mockup 5.2:
    /// "ticket-st-5369-0..."). El folio real es numérico ("1103") o el
    /// legacy con guiones ("V-20260831-67707" → se conserva tal cual).
    /// </summary>
    public string NombreArchivoPdf =>
        $"ticket-{(Tipo == TipoVenta.SorteosTec ? "st" : Tipo == TipoVenta.Lotenal ? "lot" : "ta")}-{Folio}";

    // ── Constructores desde la API ────────────────────────────────────

    /// <summary>Mapea una fila del historial (GET api/mobile/ventas).</summary>
    public static MovimientoVenta DesdeHistorial(VentaHistorialApi venta)
    {
        Helpers.FormatosFecha.TryParseFechaBackend(venta.FechaVenta, out DateOnly fecha);
        return new MovimientoVenta
        {
            IdVenta = venta.IdVenta,
            Folio = venta.Folio,
            Tipo = TipoDesdeTexto(venta.Tipo),
            Estatus = venta.Estatus,
            Fecha = fecha,
            Cliente = venta.Cliente?.Nombre is { Length: > 0 } nombre
                ? nombre.ToUpperInvariant()
                : "PÚBLICO EN GENERAL",
            Importe = venta.Total,
            Descripcion = venta.Descripcion,
            Sorteo = venta.Descripcion,
            ClienteNombrePdf = venta.Cliente?.Nombre,
            ClienteTelefonoPdf = venta.Cliente?.Telefono,
            ClienteCorreoPdf = venta.Cliente?.Correo,
        };
    }

    /// <summary>Mapea el detalle completo (GET api/mobile/ventas/{id}).</summary>
    public static MovimientoVenta DesdeDetalle(VentaDetalleApi venta)
    {
        Helpers.FormatosFecha.TryParseFechaBackend(venta.FechaVenta, out DateOnly fecha);
        DateTime fechaHora = fecha.ToDateTime(TimeOnly.MinValue);
        if (DateTime.TryParse(venta.FechaVenta, out DateTime fh))
        {
            fechaHora = fh;
        }

        List<PartidaVentaApi> partidas = venta.Partidas ?? new List<PartidaVentaApi>();
        string? boletos = partidas.Count > 0
            ? string.Join(", ", partidas.Select(p => p.NumeroBillete ?? "—"))
            : null;

        return new MovimientoVenta
        {
            IdVenta = venta.IdVenta,
            Folio = venta.Folio,
            Tipo = TipoDesdeTexto(venta.Tipo),
            Estatus = venta.Estatus,
            Fecha = fecha,
            Cliente = venta.Cliente?.Nombre is { Length: > 0 } nombre
                ? nombre.ToUpperInvariant()
                : "PÚBLICO EN GENERAL",
            Importe = venta.Total,
            Descripcion = partidas.Count > 0
                ? partidas[0].NombreSorteo ?? venta.Folio
                : venta.Folio,
            Sorteo = partidas.Count > 0 ? partidas[0].NombreSorteo : null,
            Boleto = boletos,
            Valor = partidas.Count > 0 ? partidas[0].PrecioUnitario : null,
            Partidas = partidas,
            Cedis = venta.Cedis,
            Vendedor = venta.Vendedor,
            ClienteNombrePdf = venta.Cliente?.Nombre,
            ClienteTelefonoPdf = venta.Cliente?.Telefono,
            ClienteCorreoPdf = venta.Cliente?.Correo,
            FechaHoraMovimiento = fechaHora,
        };
    }

    /// <summary>'lotenal'/'sorteos_tec'/'tiempo_aire' → TipoVenta.</summary>
    private static TipoVenta TipoDesdeTexto(string? texto) => texto switch
    {
        "sorteos_tec" => TipoVenta.SorteosTec,
        "tiempo_aire" => TipoVenta.TiempoAire,
        _ => TipoVenta.Lotenal,
    };
}
