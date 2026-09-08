using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Servicio del flujo Tickets de Venta (mockups 5, 5.1, 5.2 de Gestión).
/// Mantiene los movimientos de venta EN MEMORIA (patrón Premios y
/// Reintegros): fase SOLO INTERFAZ con 3 movimientos TEMPORALES de
/// prueba (uno por tipo) para probar listado, detalle, PDF, compartir
/// e impresión. NO son seeds ni mocks permanentes — la fuente real de
/// las ventas será el backend (ver docs/NOTAS_TICKETS_VENTA.md).
/// </summary>
public class TicketsVentaService
{
    private readonly List<MovimientoVenta> _movimientos = [];

    /// <summary>Movimientos de venta (descendente por fecha).</summary>
    public IReadOnlyList<MovimientoVenta> Movimientos()
    {
        if (_movimientos.Count == 0)
        {
            CrearMovimientosPrueba();
        }
        return _movimientos.OrderByDescending(m => m.Fecha).ToList();
    }

    /// <summary>Movimiento por folio (navegación al detalle).</summary>
    public MovimientoVenta? ObtenerPorFolio(int folio) =>
        Movimientos().FirstOrDefault(m => m.Folio == folio);

    /// <summary>
    /// Crea los 3 movimientos TEMPORALES de prueba (uno por tipo).
    /// Datos verbatim del mockup 5.2 (venta Tec) y defaults aprobados
    /// por el usuario para Lotenal y Tiempo Aire.
    /// </summary>
    private void CrearMovimientosPrueba()
    {
        // 1) Sorteos Tec — verbatim del PDF 5.2 (capa de texto)
        _movimientos.Add(new MovimientoVenta
        {
            Folio = 5369,
            Tipo = TipoVenta.SorteosTec,
            Fecha = new DateOnly(2023, 9, 1),
            Cliente = "MAURICIO LOPEZ GONZALEZ",
            Importe = 180m,
            Descripcion = "AVENTURAT ELECTRONICO 27",
            Sorteo = "AVENTURAT ELECTRONICO 27",
            Boleto = "67066",
            Valor = 180m,
            Plaza = "PUEBLA",
            Cedis = "CEDIS PUEBLA",
            Vendedor = "MAURICIO LOPEZ GONZALEZ",
            ClienteNombrePdf = "María Eugenia abrajan aparicio",
            ClienteTelefonoPdf = "2228736476",
            ClienteCorreoPdf = "maru.abrajan@hotmail.es",
            FolioCompraTec = "7448622",
            FechaHoraMovimiento = new DateTime(2026, 9, 4, 11, 12, 51),
        });

        // 2) LOTENAL — sorteo MAYOR 4024 del catálogo (11-ago-2026, $30)
        _movimientos.Add(new MovimientoVenta
        {
            Folio = 5569,
            Tipo = TipoVenta.Lotenal,
            Fecha = new DateOnly(2026, 8, 11),
            Cliente = "PÚBLICO EN GENERAL",
            Importe = 30m,
            Descripcion = "SORTEO MAYOR 4024",
            Sorteo = "SORTEO MAYOR 4024",
            Boleto = "47727",
            Valor = 30m,
            Plaza = "PUEBLA",
            Cedis = "CEDIS PUEBLA",
            Vendedor = "MAURICIO LOPEZ GONZALEZ",
            FechaHoraMovimiento = new DateTime(2026, 8, 11, 10, 30, 0),
        });

        // 3) Tiempo Aire — Telcel RECARGA $30 del catálogo
        _movimientos.Add(new MovimientoVenta
        {
            Folio = 961,
            Tipo = TipoVenta.TiempoAire,
            Fecha = new DateOnly(2026, 9, 5),
            Cliente = "PÚBLICO EN GENERAL",
            Importe = 30m,
            Descripcion = "Tiempo Aire Electrónico",
            Compania = "Telcel",
            Telefono = "2228736476",
            MontoPaquete = "$30 RECARGA",
            ClienteTelefonoPdf = "2228736476",
            Plaza = "PUEBLA",
            Cedis = "CEDIS PUEBLA",
            Vendedor = "MAURICIO LOPEZ GONZALEZ",
            FechaHoraMovimiento = new DateTime(2026, 9, 5, 9, 15, 0),
        });
    }
}
