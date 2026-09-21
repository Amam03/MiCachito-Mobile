using System.Globalization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Fila del desglose de un recibo de pago: descripción, referencia y
/// monto con signo (los negativos son documentos pagados/devoluciones).
/// Formato del mockup 8.1: cada fila se muestra con labels
/// "Descripción: / Referencia: / Monto: $ X".
/// </summary>
public class MovimientoPago
{
    /// <summary>Descripción de la operación (ej. "Lotería Nacional", "Efectivo", "FACTURA LN").</summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>Referencia del documento (ej. "SUPERIOR 2862 --> FOLIO 27373").</summary>
    public string Referencia { get; init; } = string.Empty;

    /// <summary>Monto con signo: positivo = forma de pago, negativo = documento pagado/nota.</summary>
    public decimal Monto { get; init; }

    /// <summary>Fila con label (mockup 8.1): "Descripción: Efectivo".</summary>
    public string DescripcionConLabel => $"Descripción: {Descripcion}";

    /// <summary>Fila con label (mockup 8.1): "Referencia: EFECTIVO".</summary>
    public string ReferenciaConLabel => $"Referencia: {Referencia}";

    /// <summary>Monto con label (mockup 8.1): "Monto: $ 1,420.00" / "Monto: $ -3,656.50".</summary>
    public string MontoConLabel => $"Monto: {MontoTexto}";

    /// <summary>Color del monto (mockup 8.1): rojo para documentos (negativos), oscuro para formas de pago.</summary>
    public Microsoft.Maui.Graphics.Color MontoColor => Monto < 0
        ? new Microsoft.Maui.Graphics.Color(0xC6, 0x28, 0x28)
        : new Microsoft.Maui.Graphics.Color(0x21, 0x21, 0x21);

    /// <summary>Monto formateado con 2 decimales: "$ 1,420.00" / "$ -3,656.50".</summary>
    public string MontoTexto => $"$ {Monto.ToString("#,##0.00", CultureInfo.CurrentCulture)}";

    /// <summary>Monto en valor absoluto formateado, para la tabla del PDF.</summary>
    public string MontoAbsTexto => $"$ {Math.Abs(Monto).ToString("#,##0.00", CultureInfo.CurrentCulture)}";
}

/// <summary>
/// Recibo de pago (mockup 8 de Gestión). Conectado al backend real:
/// folio_ficha (string, ej. "FP-20260527-0001") y datos del detalle
/// api/mobile/pagos/recibo/{id} (fichas aplicadas por Caja).
/// </summary>
public class ReciboPago
{
    /// <summary>Folio del recibo de caja (folio_ficha del backend).</summary>
    public string Folio { get; init; } = string.Empty;

    /// <summary>Fecha del recibo (aplicación si existe; si no, fecha de pago).</summary>
    public DateTime Fecha { get; init; }

    /// <summary>Total del recibo.</summary>
    public decimal Total { get; init; }

    /// <summary>Nombre del cliente (PDF: "Cliente: ...").</summary>
    public string Cliente { get; init; } = string.Empty;

    /// <summary>CEDIS emisor, ej. "CEDIS PBL" (PDF: "(CEDIS ...)").</summary>
    public string Cedis { get; init; } = string.Empty;

    /// <summary>Desglose de la operación (mockup 8.1).</summary>
    public IReadOnlyList<MovimientoPago> Desglose { get; init; } = Array.Empty<MovimientoPago>();

    /// <summary>
    /// Documentos pagados del PDF (tabla 1): filas de documentos reales
    /// (pagos_cartera) en valor absoluto. Si no se establece, se derivan
    /// de las filas FACTURA del desglose (comportamiento del mockup).
    /// </summary>
    public IReadOnlyList<MovimientoPago>? DocumentosPagados { get; init; }

    /// <summary>
    /// Formas de pago del PDF (tabla 2). Si no se establece: Efectivo =
    /// total del recibo y el resto $0.00 (comportamiento del mockup).
    /// El cálculo real viene del desglose del movimiento (backend).
    /// </summary>
    public IReadOnlyList<(string Forma, decimal Monto)>? FormasPago { get; init; }

    /// <summary>Folio para mostrar: "Folio: FP-20260527-0001".</summary>
    public string FolioTexto => $"Folio: {Folio}";

    /// <summary>Fecha para mostrar/lista: "28-octubre-2025" (dd-MMMM-yyyy, es-MX fijo).</summary>
    public string FechaTexto => Helpers.FormatosFecha.FechaLarga(Fecha);

    /// <summary>Total para mostrar: "$4,280.00".</summary>
    public string TotalTexto => $"${Total.ToString("#,##0.00", CultureInfo.CurrentCulture)}";

    /// <summary>Fecha del PDF de referencia: "2026-05-21" (yyyy-MM-dd).</summary>
    public string FechaPdf => Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Nombre base del archivo PDF: "pago-FP-20260527-0001-21-05-2026" (mockup 8.3).</summary>
    public string NombreArchivoPdf => $"pago-{Folio}-{Fecha:dd-MM-yyyy}";

    /// <summary>Documentos pagados (PDF tabla 1): los establecidos o las filas FACTURA del desglose.</summary>
    public IReadOnlyList<MovimientoPago> DocumentosPagadosEfectivos =>
        (DocumentosPagados ?? Desglose
            .Where(m => m.Descripcion.Contains("FACTURA", StringComparison.OrdinalIgnoreCase))
            .ToList());

    /// <summary>Total de documentos pagados (PDF: "Total Documentos: $ 6,170.00").</summary>
    public decimal TotalDocumentos => DocumentosPagadosEfectivos.Sum(m => Math.Abs(m.Monto));

    /// <summary>Formas de pago del PDF (tabla 2): las establecidas o el fallback del mockup.</summary>
    public IReadOnlyList<(string Forma, decimal Monto)> FormasPagoEfectivas =>
        FormasPago ?? new List<(string, decimal)>
        {
            ("Efectivo", Total),
            ("Depósitos/Transferencias", 0m),
            ("Premios", 0m),
            ("Reintegros", 0m),
            ("Lotería Instántanea", 0m),
            ("Cheques", 0m),
            ("Notas de Crédito", 0m),
        };
}
