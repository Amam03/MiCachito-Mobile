using System.Text.Json.Serialization;

namespace MiCachito.Mobile.Models.Responses;

/// <summary>
/// Respuesta de GET api/mobile/ventas/sorteos (F1): dotaciones de
/// Lotería Nacional con material LIVE asignado al billetero de la
/// sesión (estatus='asignado', productos.id_categoria=1).
/// </summary>
public sealed class MobileVentasSorteosApi
{
    /// <summary>Dotaciones del billetero, fecha descendente.</summary>
    [JsonPropertyName("sorteos")]
    public List<DotacionApi>? Sorteos { get; set; }
}

/// <summary>Fila de dotación (lista y encabezado del detalle comparten formato).</summary>
public sealed class DotacionApi
{
    /// <summary>Id del sorteo.</summary>
    [JsonPropertyName("id_sorteo")]
    public int IdSorteo { get; set; }

    /// <summary>Número de dotación; null = material legacy sin número.</summary>
    [JsonPropertyName("numero_sorteo")]
    public string? NumeroSorteo { get; set; }

    /// <summary>Label "NOMBRE - NUMERO" (o solo NOMBRE sin dotación).</summary>
    [JsonPropertyName("sorteo")]
    public string? Sorteo { get; set; }

    /// <summary>Nombre del producto LN sin prefijo, MAYÚSCULAS ("MAYOR").</summary>
    [JsonPropertyName("nombre_producto")]
    public string? NombreProducto { get; set; }

    /// <summary>Fecha de celebración cruda del backend ("yyyy-MM-dd").</summary>
    [JsonPropertyName("fecha_sorteo")]
    public string? FechaSorteo { get; set; }

    /// <summary>Tipo de sorteo (enum sorteos.tipo_sorteo).</summary>
    [JsonPropertyName("tipo_sorteo")]
    public string? TipoSorteo { get; set; }

    /// <summary>Subcódigo de 3 dígitos del barcode (ediciones).</summary>
    [JsonPropertyName("subcodigo_sorteo")]
    public string? SubcodigoSorteo { get; set; }

    /// <summary>Fracciones libres totales de la dotación.</summary>
    [JsonPropertyName("fracciones_disponibles")]
    public int FraccionesDisponibles { get; set; }

    /// <summary>
    /// Precio por cachito. Llega como STRING ("70.00"): los decimales
    /// de MySQL viajan como string en el JSON de Yii2 (verificado en
    /// vivo, F1); se parsea InvariantCulture en el servicio.
    /// </summary>
    [JsonPropertyName("precio_fraccion")]
    public string? PrecioFraccion { get; set; }
}

/// <summary>
/// Respuesta de GET api/mobile/ventas/billetes?id_sorteo=y
/// numero_sorteo=X (F1): detalle de UNA dotación del billetero.
/// </summary>
public sealed class MobileVentasBilletesApi
{
    /// <summary>Encabezado de la dotación consultada.</summary>
    [JsonPropertyName("sorteo")]
    public DotacionApi? Sorteo { get; set; }

    /// <summary>Series con fracciones libres, agrupadas por (numero_billete, serie, signo).</summary>
    [JsonPropertyName("series")]
    public List<SerieApi>? Series { get; set; }

    /// <summary>Total de fracciones libres de la dotación.</summary>
    [JsonPropertyName("total_fracciones")]
    public int TotalFracciones { get; set; }
}

/// <summary>Serie de la dotación con sus fracciones libres.</summary>
public sealed class SerieApi
{
    /// <summary>Número de billete ("0102").</summary>
    [JsonPropertyName("numero_billete")]
    public string? NumeroBillete { get; set; }

    /// <summary>Serie física de billetes_loteria ("01").</summary>
    [JsonPropertyName("serie")]
    public string? Serie { get; set; }

    /// <summary>Código de signo (zodiaco); vacío si no aplica.</summary>
    [JsonPropertyName("signo")]
    public string? Signo { get; set; }

    /// <summary>Nombre de signo ("GEMINIS"); vacío si no aplica.</summary>
    [JsonPropertyName("signo_nombre")]
    public string? SignoNombre { get; set; }

    /// <summary>Fracciones libres de la serie.</summary>
    [JsonPropertyName("fracciones_disponibles")]
    public int FraccionesDisponibles { get; set; }

    /// <summary>Precio por cachito (string decimal MySQL, ver DotacionApi).</summary>
    [JsonPropertyName("precio_fraccion")]
    public string? PrecioFraccion { get; set; }

    /// <summary>Fracciones libres con id_billete exacto (venta F3 por id_billete).</summary>
    [JsonPropertyName("fracciones")]
    public List<FraccionApi>? Fracciones { get; set; }
}

/// <summary>Fracción libre individual de una serie.</summary>
public sealed class FraccionApi
{
    /// <summary>PK de la fila de billetes_loteria.</summary>
    [JsonPropertyName("id_billete")]
    public int IdBillete { get; set; }

    /// <summary>Fracción/vigésimo dentro del billete (0-19).</summary>
    [JsonPropertyName("fraccion")]
    public int Fraccion { get; set; }
}

/// <summary>
/// Respuesta de POST api/mobile/ventas/crear (F3): la venta creada.
/// El total viene calculado por el backend con el precio REAL
/// (sorteos.precio_fraccion); el folio es el identificador para el
/// usuario.
/// </summary>
public sealed class VentaCreadaApi
{
    /// <summary>PK de la venta (ventas.id_venta).</summary>
    [JsonPropertyName("id_venta")]
    public int IdVenta { get; set; }

    /// <summary>Folio de la venta ("V-20260925-67242").</summary>
    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>Total real de la venta (calculado en backend).</summary>
    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}

/// <summary>
/// Item de devolución (F4): los campos del QR del boleto parseado.
/// El backend resuelve el id_billete exacto por esta combinación y
/// valida que esté asignado al billetero de la sesión.
/// </summary>
public sealed class ItemDevolucionApi
{
    /// <summary>Número de boleto ("0101").</summary>
    [JsonPropertyName("numero_billete")]
    public string NumeroBillete { get; set; } = string.Empty;

    /// <summary>Serie física ("01"); vacía si no aplica (zodiaco).</summary>
    [JsonPropertyName("serie")]
    public string? Serie { get; set; }

    /// <summary>Fracción/vigésimo del cachito (0-19).</summary>
    [JsonPropertyName("fraccion")]
    public int? Fraccion { get; set; }

    /// <summary>Código de signo zodiacal (zodiaco); null si no aplica.</summary>
    [JsonPropertyName("signo_codigo")]
    public string? SignoCodigo { get; set; }

    /// <summary>Dotación (billetes_loteria.numero_sorteo, "4025").</summary>
    [JsonPropertyName("numero_sorteo")]
    public string? NumeroSorteo { get; set; }
}

/// <summary>
/// Respuesta de POST api/mobile/ventas/devolver (F4): el movimiento de
/// devolución creado (pendiente, LIVE inmediato) con los id_billete
/// exactos afectados.
/// </summary>
public sealed class DevolucionCreadaApi
{
    /// <summary>PK del movimiento (movimientos_almacen.id_movimiento).</summary>
    [JsonPropertyName("id_movimiento")]
    public int IdMovimiento { get; set; }

    /// <summary>Folio del movimiento ("MA-20260928-0002").</summary>
    [JsonPropertyName("folio")]
    public string Folio { get; set; } = string.Empty;

    /// <summary>Estatus administrativo ("pendiente" hasta liquidación Desktop).</summary>
    [JsonPropertyName("estatus")]
    public string Estatus { get; set; } = string.Empty;

    /// <summary>id_billete exactos devueltos (trazabilidad).</summary>
    [JsonPropertyName("billetes")]
    public List<int> Billetes { get; set; } = new();

    /// <summary>Cachitos devueltos.</summary>
    [JsonPropertyName("total_cachitos")]
    public int TotalCachitos { get; set; }
}

// ── Sorteos TEC ───────────────────────────────────────────────────────────────
// Contrato de api/mobile/ventas/sorteos-tec y .../billetes-tec. Rutas
// PROPIAS porque TEC no se fracciona: no hay dotaciones con numero_sorteo,
// ni series, ni fracciones libres. Se vende por BOLETO COMPLETO.

/// <summary>
/// Respuesta de GET api/mobile/ventas/sorteos-tec: sorteos TEC con material
/// asignado al billetero de la sesion (productos.id_categoria = 4).
/// </summary>
public sealed class SorteosTecApi
{
    /// <summary>Sorteos TEC disponibles, fecha descendente.</summary>
    [JsonPropertyName("sorteos")]
    public List<SorteoTecApi>? Sorteos { get; set; }
}

/// <summary>Fila de la lista de sorteos TEC.</summary>
public sealed class SorteoTecApi
{
    /// <summary>Id del sorteo (sorteos.id_sorteo).</summary>
    [JsonPropertyName("id_sorteo")]
    public int IdSorteo { get; set; }

    /// <summary>Nombre del sorteo tal cual la BD ("Sorteo Mi Sueño").</summary>
    [JsonPropertyName("nombre_sorteo")]
    public string? NombreSorteo { get; set; }

    /// <summary>Fecha del sorteo ("yyyy-MM-dd").</summary>
    [JsonPropertyName("fecha_sorteo")]
    public string? FechaSorteo { get; set; }

    /// <summary>Tipo (sorteos.tipo_sorteo = "sorteos_tec").</summary>
    [JsonPropertyName("tipo_sorteo")]
    public string? TipoSorteo { get; set; }

    /// <summary>
    /// Precio del BOLETO COMPLETO. Llega como STRING ("490.00"): los
    /// decimales de MySQL viajan como string en el JSON de Yii2 (igual que
    /// precio_fraccion en LN); se parsea en el servicio.
    /// </summary>
    [JsonPropertyName("precio")]
    public string? Precio { get; set; }

    /// <summary>Boletos disponibles del billetero en ese sorteo.</summary>
    [JsonPropertyName("boletos_disponibles")]
    public int BoletosDisponibles { get; set; }
}

/// <summary>
/// Respuesta de GET api/mobile/ventas/billetes-tec?id_sorteo=
/// </summary>
public sealed class BilletesTecApi
{
    /// <summary>Encabezado del sorteo (nombre y precio del boleto).</summary>
    [JsonPropertyName("sorteo")]
    public SorteoTecEncabezadoApi? Sorteo { get; set; }

    /// <summary>Boletos vendibles del billetero en ese sorteo.</summary>
    [JsonPropertyName("billetes")]
    public List<BilleteTecApi>? Billetes { get; set; }

    /// <summary>Total de boletos disponibles.</summary>
    [JsonPropertyName("total_billetes")]
    public int TotalBilletes { get; set; }
}

/// <summary>Encabezado del sorteo TEC consultado.</summary>
public sealed class SorteoTecEncabezadoApi
{
    /// <summary>Id del sorteo.</summary>
    [JsonPropertyName("id_sorteo")]
    public int IdSorteo { get; set; }

    /// <summary>Nombre del sorteo.</summary>
    [JsonPropertyName("nombre_sorteo")]
    public string? NombreSorteo { get; set; }

    /// <summary>Precio del boleto completo (string decimal).</summary>
    [JsonPropertyName("precio")]
    public string? Precio { get; set; }
}

/// <summary>Un boleto TEC vendible (una unidad, sin fracciones).</summary>
public sealed class BilleteTecApi
{
    /// <summary>Id exacto que se envia al crear la venta.</summary>
    [JsonPropertyName("id_billete")]
    public int IdBillete { get; set; }

    /// <summary>Numero impreso del boleto.</summary>
    [JsonPropertyName("numero_billete")]
    public string? NumeroBillete { get; set; }
}
