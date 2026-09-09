using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Estado en memoria del flujo Recibos de Pago (mockups 8.x de Gestión).
/// Fase solo-interfaz: UN único registro temporal para validar el flujo
/// (patrón Premios y Reintegros 3.x); los datos del mockup son solo
/// referencia visual. Fuente real: backend (docs/NOTAS_RECIBOS_PAGO.md).
/// </summary>
public class RecibosPagoService
{
    private readonly ReciboPago _temporal = new()
    {
        // TEMPORAL — datos del mockup 8.1/8.3 para probar el flujo.
        Folio = 41868,
        Fecha = new DateTime(2025, 10, 28),
        Total = 4280m,
        Cliente = "MAURICIO LOPEZ GONZALEZ",
        Cedis = "CEDIS PBL",
        Desglose = new List<MovimientoPago>
        {
            new() { Descripcion = "Lotería Nacional", Referencia = "Lotería Nacional", Monto = 1420m },
            new() { Descripcion = "Efectivo", Referencia = "EFECTIVO", Monto = 2860m },
            new() { Descripcion = "FACTURA LN", Referencia = "SUPERIOR 2862 - FOLIO 27373", Monto = -3656.50m },
            new() { Descripcion = "FACTURA LN", Referencia = "ZODIACO ESPECIAL 1724 - FOLIO 27429", Monto = -623.50m },
        },
    };

    /// <summary>Recibos en orden cronológico inverso (mockup 8).</summary>
    public IReadOnlyList<ReciboPago> Recibos() => new[] { _temporal };

    /// <summary>Busca un recibo por folio.</summary>
    public ReciboPago? ObtenerPorFolio(int folio) => Recibos().FirstOrDefault(r => r.Folio == folio);
}
