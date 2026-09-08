namespace MiCachito.Mobile.Services.Scanning;

/// <summary>
/// Informacion del sorteo identificado a partir del codigo escaneado.
/// </summary>
public sealed class SorteoInfo
{
    public string Clave { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string? NumeroSorteo { get; init; }

    /// <summary>True si el subcodigo corresponde a una edicion cotejada contra QRs reales.</summary>
    public bool EdicionCotejada { get; init; }
}

/// <summary>
/// Identificacion del sorteo a partir del codigo parseado.
/// Port de src/utils/sorteo_identificador.py del desktop.
///
/// Tabla de subcodigos del desktop (regla primaria):
///   140=Mayor, 261=Superior, 516=Especial, 394=Magno, 446=Zodiaco, 448=ZodiacoEsp.
///
/// LIMITACION VERIFICADA (cotejo con QRs reales, sep-2026): el subcodigo del QR
/// es el codigo de EDICION del sorteo, no un ID fijo del producto. Cambia con
/// cada edicion (ej: Mayor 4024 -> 140, Mayor 4010 -> 126; Zodiaco 1756 -> 446,
/// Zodiaco 1751 -> 441). La tabla del desktop cubre las ediciones de las
/// muestras con que se construyo; el catalogo de ediciones de abajo agrega las
/// ediciones reales decodificadas de los boletos de docs/UI.
/// </summary>
public static class SorteoIdentificador
{
    /// <summary>Tabla de subcodigos verbatim del desktop (subcodigo -> sorteo).</summary>
    public static readonly IReadOnlyDictionary<string, (string Clave, string Nombre, int Longitud)> SubcodigosSorteos =
        new Dictionary<string, (string, string, int)>
        {
            ["140"] = ("mayor", "Sorteo Mayor", 35),
            ["261"] = ("superior", "Sorteo Superior", 35),
            ["516"] = ("especial", "Sorteo Especial", 35),
            ["394"] = ("magno", "Sorteo Magno", 35),
            ["446"] = ("zodiaco", "Sorteo Zodiaco", 34),
            ["448"] = ("zodiaco_especial", "Sorteo Zodiaco Especial", 34),
        };

    /// <summary>
    /// Ediciones reales cotejadas contra los QRs de los boletos de docs/UI
    /// (subcodigo -> (producto, numero de sorteo impreso en el billete)).
    /// Magno 392 -> 394 y Especial -> 516 provienen de las muestras tecleadas
    /// del backend (docs "Escaneo de billetes por serie").
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Clave, string Nombre, string NumeroSorteo)> EdicionesCotejadas =
        new Dictionary<string, (string, string, string)>
        {
            ["126"] = ("mayor", "Sorteo Mayor", "4010"),
            ["140"] = ("mayor", "Sorteo Mayor", "4024"),
            ["249"] = ("superior", "Sorteo Superior", "2881"),
            ["261"] = ("superior", "Sorteo Superior", "2893"),
            ["394"] = ("magno", "Sorteo Magno", "392"),
            ["441"] = ("zodiaco", "Sorteo Zodiaco", "1751"),
            ["446"] = ("zodiaco", "Sorteo Zodiaco", "1756"),
            ["448"] = ("zodiaco_especial", "Sorteo Zodiaco Especial", "1758"),
            ["516"] = ("especial", "Sorteo Especial", "NA"),
        };

    /// <summary>
    /// Identifica el sorteo a partir del parseo. Port de identificar_sorteo_por_subcodigo:
    /// tabla del desktop primero; si no esta, catalogo de ediciones cotejadas.
    /// Para TEC/Universitarios/Rasca-Prorra usa el nombre del parseo.
    /// </summary>
    public static SorteoInfo? Identificar(BilleteParseado parseado)
    {
        if (parseado.EsSorteoTec)
        {
            return new SorteoInfo { Clave = "tec", Nombre = parseado.NombreSorteoTec ?? "Sorteo TEC" };
        }

        if (parseado.EsSorteoUniversitario)
        {
            return new SorteoInfo { Clave = "universitario", Nombre = "Sorteo Universitario" };
        }

        if (parseado.EsRascaProrra)
        {
            return new SorteoInfo { Clave = "rasca_prorra", Nombre = parseado.NombreProductoRascaProrra ?? "Rasca/Prorra" };
        }

        string? sub = parseado.SubCodigo;
        if (string.IsNullOrEmpty(sub))
        {
            return null;
        }

        if (SubcodigosSorteos.TryGetValue(sub, out var info) && info.Longitud == parseado.LongitudTotal)
        {
            return new SorteoInfo { Clave = info.Clave, Nombre = info.Nombre };
        }

        if (EdicionesCotejadas.TryGetValue(sub, out var ed))
        {
            return new SorteoInfo { Clave = ed.Clave, Nombre = ed.Nombre, NumeroSorteo = ed.NumeroSorteo, EdicionCotejada = true };
        }

        return null;
    }
}
