using System.Globalization;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Fila del desglose de un recibo de pago: descripción, referencia y
/// monto con signo (los negativos son devoluciones/notas).
/// </summary>
public class MovimientoPago
{
    /// <summary>Descripción de la operación (ej. "Lotería Nacional", "Efectivo", "FACTURA LN").</summary>
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>Referencia del documento (ej. "SUPERIOR 2862 - FOLIO 27373").</summary>
    public string Referencia { get; init; } = string.Empty;

    /// <summary>Monto con signo: positivo = cobro, negativo = devolución/nota.</summary>
    public decimal Monto { get; init; }

    /// <summary>Monto formateado con 2 decimales: "$ 1,420.00" / "$ -3,656.50".</summary>
    public string MontoTexto => $"$ {Monto.ToString("#,##0.00", CultureInfo.CurrentCulture)}";

    /// <summary>Monto en valor absoluto formateado, para la tabla del PDF.</summary>
    public string MontoAbsTexto => $"$ {Math.Abs(Monto).ToString("#,##0.00", CultureInfo.CurrentCulture)}";
}

/// <summary>
/// Recibo de pago (mockup 8 de Gestión). Fase solo-interfaz: un único
/// registro temporal en memoria para validar el flujo; sustituir por
/// datos reales del backend (ver docs/NOTAS_RECIBOS_PAGO.md).
/// </summary>
public class ReciboPago
{
    /// <summary>Folio del recibo de caja.</summary>
    public int Folio { get; init; }

    /// <summary>Fecha del recibo.</summary>
    public DateTime Fecha { get; init; }

    /// <summary>Total del recibo.</summary>
    public decimal Total { get; init; }

    /// <summary>Nombre del cliente (PDF: "Cliente: ...").</summary>
    public string Cliente { get; init; } = string.Empty;

    /// <summary>CEDIS emisor, ej. "CEDIS PBL" (PDF: "(CEDIS ...)").</summary>
    public string Cedis { get; init; } = string.Empty;

    /// <summary>Desglose de la operación (mockup 8.1).</summary>
    public IReadOnlyList<MovimientoPago> Desglose { get; init; } = Array.Empty<MovimientoPago>();

    /// <summary>Folio para mostrar: "Folio: 41868".</summary>
    public string FolioTexto => $"Folio: {Folio}";

    /// <summary>Fecha para mostrar/lista: "28-octubre-2025" (dd-MMMM-yyyy, es-MX fijo).</summary>
    public string FechaTexto => Helpers.FormatosFecha.FechaLarga(Fecha);

    /// <summary>Total para mostrar: "$4,280.00".</summary>
    public string TotalTexto => $"${Total.ToString("#,##0.00", CultureInfo.CurrentCulture)}";

    /// <summary>Fecha del PDF de referencia: "2026-05-21" (yyyy-MM-dd).</summary>
    public string FechaPdf => Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>Nombre base del archivo PDF: "pago-48489-21-05-2026" (mockup 8.3).</summary>
    public string NombreArchivoPdf => $"pago-{Folio}-{Fecha:dd-MM-yyyy}";

    /// <summary>
    /// Documentos pagados del PDF (tabla 1): filas FACTURA del desglose
    /// (montos en absoluto). En el mockup: 3656.50 + 623.50 = 4,280.00.
    /// </summary>
    public IReadOnlyList<MovimientoPago> DocumentosPagados =>
        Desglose.Where(m => m.Descripcion.Contains("FACTURA", StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Total de documentos pagados (PDF: "Total Documentos: $ 6,170.00").</summary>
    public decimal TotalDocumentos => DocumentosPagados.Sum(m => Math.Abs(m.Monto));

    /// <summary>
    /// Formas de pago del PDF (tabla 2). Fase solo-interfaz: Efectivo =
    /// total del recibo y el resto $0.00, como el PDF de referencia
    /// (la tabla cuadra con Total Documentos). El cálculo real por
    /// categoría vendrá del backend (docs/NOTAS_RECIBOS_PAGO.md).
    /// </summary>
    public IReadOnlyList<(string Forma, decimal Monto)> FormasPago
    {
        get
        {
            return new List<(string, decimal)>
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
    }
}
