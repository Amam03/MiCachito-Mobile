namespace MiCachito.Mobile.Services.Scanning;

/// <summary>
/// Resultado del parseo de un codigo de boleto escaneado.
/// Port fiel de parsear_cadena() del desktop (src/utils/billete_parser.py).
/// </summary>
public sealed class BilleteParseado
{
    public string CodigoCompleto { get; init; } = string.Empty;
    public string PrefijoEmisor { get; init; } = string.Empty;
    public string? NumeroBillete { get; init; }
    public string? Serie { get; init; }
    public int SerieSabana { get; init; }
    public string? SubCodigo { get; init; }
    public string? SignoCodigo { get; init; }
    public string? SignoNombre { get; init; }
    public int? Fraccion { get; init; }
    public int? Vigesimo { get; init; }
    public string? DigitoVerificacion { get; init; }
    public string? FechaSorteo { get; init; }
    public string? FechaRaw { get; init; }
    public int LongitudTotal { get; init; }
    public bool EsSorteoTec { get; init; }
    public bool EsSorteoUniversitario { get; init; }
    public bool EsRascaProrra { get; init; }
    public bool EsProrrapido { get; init; }
    public string? NombreSorteoTec { get; init; }
    public string? CodigoSorteoInterno { get; init; }
    public string? TipoCodigoTec { get; init; }
    public string? NombreProductoRascaProrra { get; init; }

    /// <summary>True si es Loteria Nacional (34/35 digitos).</summary>
    public bool EsLoteriaNacional => !EsSorteoTec && !EsSorteoUniversitario && !EsRascaProrra;

    /// <summary>True si es Zodiaco (34 digitos, sin serie; el signo hace de serie).</summary>
    public bool EsZodiaco => EsLoteriaNacional && Serie is null;
}

/// <summary>
/// Parser unificado del codigo de barras/QR de los boletos.
/// Port de src/utils/billete_parser.py del sistema de escritorio.
/// Orden de deteccion: LN (34/35 dig) - TEC (SE/CL/SS) - Universitarios (6 dig)
/// - Rasca/Prorra (12/22 dig con prefijo de 3 conocido).
/// </summary>
public static class BilleteParser
{
    public const int LongitudCodigoBillete = 34;

    public static readonly int[] LongitudesValidasBillete = { 34, 35 };

    public const string DescripcionLongitudesBillete = "34 o 35";

    /// <summary>Signos zodiacales 01-12, verbatim del desktop (sin acentos).</summary>
    public static readonly IReadOnlyDictionary<string, string> SerieASigno = new Dictionary<string, string>
    {
        ["01"] = "ARIES",
        ["02"] = "TAURO",
        ["03"] = "GEMINIS",
        ["04"] = "CANCER",
        ["05"] = "LEO",
        ["06"] = "VIRGO",
        ["07"] = "LIBRA",
        ["08"] = "ESCORPION",
        ["09"] = "SAGITARIO",
        ["10"] = "CAPRICORNIO",
        ["11"] = "ACUARIO",
        ["12"] = "PISCIS",
    };

    /// <summary>Prefijos TEC verbatim del desktop.</summary>
    public static readonly IReadOnlyDictionary<string, string> SorteosTecPrefijos = new Dictionary<string, string>
    {
        ["SE"] = "Sorteo Educativo",
        ["CL"] = "Sorteo Dinero de X Vida",
        ["SS"] = "Sorteo Mi Sueño",
    };

    /// <summary>Prefijos Rasca/Prorra verbatim del desktop (prefijo, longitud, nombre).</summary>
    public static readonly IReadOnlyDictionary<string, (int Longitud, string Nombre)> PrefijoRascaProrraInfo = new Dictionary<string, (int, string)>
    {
        ["232"] = (12, "Rasca 10"),
        ["228"] = (12, "Rasca 20"),
        ["239"] = (12, "Rasca 20"),
        ["204"] = (12, "Rasca 30"),
        ["351"] = (22, "Prorrapido 10"),
        ["353"] = (22, "Prorrapido 20"),
        ["357"] = (22, "Prorrapido 30"),
        ["366"] = (22, "Prorrapido 50"),
    };

    private static readonly int[] LongitudesTecValidas = { 9, 10, 11, 12 };
    private static readonly int[] LongitudesUniversitario = { 6 };
    private static readonly int[] LongitudesRascaProrra = { 12, 22 };

    /// <summary>
    /// Parser unificado: detecta el tipo de codigo en el mismo orden que el
    /// desktop (LN - TEC - Universitarios - Rasca/Prorra). Null si no aplica.
    /// </summary>
    public static BilleteParseado? ParsearCadena(string cadena)
    {
        cadena = cadena.Trim();
        if (cadena.Length == 0)
        {
            return null;
        }

        if (EsSoloDigitos(cadena) && LongitudesValidasBillete.Contains(cadena.Length))
        {
            return ParsearCadenaBillete(cadena);
        }

        if (EsCodigoSorteoTec(cadena))
        {
            return ParsearCodigoTec(cadena);
        }

        if (EsCodigoUniversitario(cadena))
        {
            return ParsearCodigoUniversitario(cadena);
        }

        if (EsCodigoRascaProrra(cadena))
        {
            return ParsearCodigoRascaProrra(cadena);
        }

        return null;
    }

    private static bool EsSoloDigitos(string s)
    {
        foreach (char c in s)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }
        return s.Length > 0;
    }

    /// <summary>LN 34/35 digitos. Port de parsear_cadena_billete.</summary>
    private static BilleteParseado? ParsearCadenaBillete(string cadena)
    {
        string bloqueEmisor = cadena[..^11];
        string vigesimoRaw = cadena[^11..^8];
        string fechaRaw = cadena[^8..];

        int fraccionInt = int.Parse(vigesimoRaw) / 10;
        if (fraccionInt is < 1 or > 20)
        {
            return null;
        }

        string digitoVerificacion = vigesimoRaw[^1..];
        string dia = fechaRaw[0..2];
        string mes = fechaRaw[2..4];
        string anio = fechaRaw[4..8];

        if (cadena.Length == 35)
        {
            string numeroBillete = SinCerosIzquierda(bloqueEmisor[14..19]);
            string serie = bloqueEmisor[19..21];
            return new BilleteParseado
            {
                CodigoCompleto = cadena,
                PrefijoEmisor = bloqueEmisor[..14],
                NumeroBillete = numeroBillete,
                Serie = serie,
                SerieSabana = int.Parse(serie) * 1000,
                SubCodigo = bloqueEmisor[21..24],
                Fraccion = fraccionInt,
                Vigesimo = fraccionInt,
                DigitoVerificacion = digitoVerificacion,
                FechaSorteo = $"{anio}-{mes}-{dia}",
                FechaRaw = fechaRaw,
                LongitudTotal = cadena.Length,
            };
        }

        string billeteZodiaco = SinCerosIzquierda(bloqueEmisor[14..18]);
        string signoCodigo = bloqueEmisor[18..20];
        if (!SerieASigno.ContainsKey(signoCodigo))
        {
            return null;
        }
        string signoNombre = SerieASigno[signoCodigo];
        return new BilleteParseado
        {
            CodigoCompleto = cadena,
            PrefijoEmisor = bloqueEmisor[..14],
            NumeroBillete = billeteZodiaco,
            Serie = null,
            SerieSabana = int.Parse(signoCodigo) * 1000,
            SubCodigo = bloqueEmisor[20..23],
            SignoCodigo = signoCodigo,
            SignoNombre = signoNombre,
            Fraccion = fraccionInt,
            Vigesimo = fraccionInt,
            DigitoVerificacion = digitoVerificacion,
            FechaSorteo = $"{anio}-{mes}-{dia}",
            FechaRaw = fechaRaw,
            LongitudTotal = cadena.Length,
        };
    }

    private static string SinCerosIzquierda(string s)
    {
        string t = s.TrimStart('0');
        return t.Length > 0 ? t : "0";
    }

    /// <summary>Port de es_codigo_sorteo_tec.</summary>
    public static bool EsCodigoSorteoTec(string cadena)
    {
        string upper = cadena.Trim().ToUpperInvariant();
        if (upper.Length < 9)
        {
            return false;
        }
        return SorteosTecPrefijos.ContainsKey(upper[..2]);
    }

    /// <summary>Port de parsear_codigo_tec: [Prefijo 2][Codigo 3][T|B][Billete].</summary>
    private static BilleteParseado? ParsearCodigoTec(string cadena)
    {
        string upper = cadena.Trim().ToUpperInvariant();
        if (!LongitudesTecValidas.Contains(upper.Length))
        {
            return null;
        }

        string prefijo = upper[..2];
        if (!SorteosTecPrefijos.ContainsKey(prefijo))
        {
            return null;
        }

        if (upper.Length < 9)
        {
            return null;
        }

        string codigoSorteoInterno = upper[2..5];
        char tipoChar = upper[5];
        if (tipoChar is not ('T' or 'B'))
        {
            return null;
        }

        string numBilleteStr = upper[6..];
        if (!EsSoloDigitos(numBilleteStr))
        {
            return null;
        }

        return new BilleteParseado
        {
            CodigoCompleto = upper,
            EsSorteoTec = true,
            PrefijoEmisor = prefijo,
            NombreSorteoTec = SorteosTecPrefijos[prefijo],
            CodigoSorteoInterno = codigoSorteoInterno,
            TipoCodigoTec = tipoChar.ToString(),
            NumeroBillete = int.Parse(numBilleteStr).ToString(),
            Serie = null,
            SerieSabana = 0,
            SubCodigo = codigoSorteoInterno,
            LongitudTotal = upper.Length,
        };
    }

    /// <summary>Port de es_codigo_sorteo_universitario.</summary>
    public static bool EsCodigoUniversitario(string cadena)
    {
        cadena = cadena.Trim();
        if (!EsSoloDigitos(cadena))
        {
            return false;
        }
        return LongitudesUniversitario.Contains(cadena.Length);
    }

    /// <summary>Port de parsear_codigo_universitario: el numero completo es el id.</summary>
    private static BilleteParseado? ParsearCodigoUniversitario(string cadena)
    {
        return new BilleteParseado
        {
            CodigoCompleto = cadena,
            EsSorteoUniversitario = true,
            NumeroBillete = int.Parse(cadena).ToString(),
            Serie = null,
            SerieSabana = 0,
            LongitudTotal = cadena.Length,
        };
    }

    /// <summary>Port de es_codigo_sorteo_rasca_prorra.</summary>
    public static bool EsCodigoRascaProrra(string cadena)
    {
        cadena = cadena.Trim();
        if (!EsSoloDigitos(cadena))
        {
            return false;
        }
        if (!LongitudesRascaProrra.Contains(cadena.Length))
        {
            return false;
        }
        return PrefijoRascaProrraInfo.ContainsKey(cadena[..3]);
    }

    /// <summary>
    /// Port de parsear_codigo_rasca_prorra.
    /// Rasca 12 dig: posicion = ultimos 3. Prorra 22 dig: posicion = [9:12].
    /// </summary>
    private static BilleteParseado? ParsearCodigoRascaProrra(string cadena)
    {
        cadena = cadena.Trim();
        string prefijo = cadena[..3];
        if (!PrefijoRascaProrraInfo.TryGetValue(prefijo, out var info))
        {
            return null;
        }

        if (info.Longitud == 12 && cadena.Length == 12)
        {
            return new BilleteParseado
            {
                CodigoCompleto = cadena,
                EsRascaProrra = true,
                EsProrrapido = false,
                PrefijoEmisor = prefijo,
                NombreProductoRascaProrra = info.Nombre,
                NumeroBillete = int.Parse(cadena[^3..]).ToString(),
                Serie = null,
                SerieSabana = 0,
                LongitudTotal = cadena.Length,
            };
        }

        if (info.Longitud == 22 && cadena.Length == 22)
        {
            return new BilleteParseado
            {
                CodigoCompleto = cadena,
                EsRascaProrra = true,
                EsProrrapido = true,
                PrefijoEmisor = prefijo,
                NombreProductoRascaProrra = info.Nombre,
                NumeroBillete = int.Parse(cadena[9..12]).ToString(),
                Serie = null,
                SerieSabana = 0,
                LongitudTotal = cadena.Length,
            };
        }

        return null;
    }
}
