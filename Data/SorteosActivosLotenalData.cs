using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

/// <summary>
/// Catálogo de sorteos de Lotería Nacional por tipo.
/// Datos extraídos del calendario oficial LOTENAL:
/// https://www.loterianacional.gob.mx/Billete/CalendarioSorteos
/// (API: POST /Billete/JsCalendarioDatos?Idcalendario=1)
///
/// Incluye sorteos pasados y futuros. El ViewModel filtra por EstaDisponible
/// (fecha de celebración >= hoy) para mostrar solo los vigentes.
///
/// En producción se consultarán via GET /api/sorteos?vigente=1
/// (scope pendientes: vigente=1 AND celebrado=0 AND fecha_sorteo >= today).
/// </summary>
public static class SorteosActivosLotenalData
{
    /// <summary>
    /// Devuelve TODOS los sorteos para el tipo (Id) seleccionado,
    /// incluyendo los que ya pasaron. El ViewModel filtra por EstaDisponible.
    /// </summary>
    public static IReadOnlyList<SorteoActivoLotenal> ObtenerPorTipoId(int tipoId)
    {
        return _sorteos.TryGetValue(tipoId, out var lista)
            ? lista
            : [];
    }

    // ── Datos comunes por tipo ─────────────────────────────────────────
    private const string L1_Mayor  = "SORTEO";
    private const string L2_Mayor  = "MAYOR";
    private const string N_Mayor   = "SORTEO MAYOR";
    private const decimal P_Mayor  = 30m;
    private const string C_Mayor   = "#FFB600";

    private const string L1_Sup    = "SORTEO";
    private const string L2_Sup    = "SUPERIOR";
    private const string N_Sup     = "SORTEO SUPERIOR";
    private const decimal P_Sup    = 40m;
    private const string C_Sup     = "#4D9D2E";

    private const string L1_Zod    = "SORTEO";
    private const string L2_Zod    = "ZODIACO";
    private const string N_Zod     = "SORTEO ZODIACO";
    private const decimal P_Zod    = 20m;
    private const string C_Zod     = "#ED40A9";

    private const string L1_ZodE   = "SORTEO ZODIACO";
    private const string L2_ZodE   = "ESPECIAL";
    private const string N_ZodE    = "SORTEO ZODIACO ESPECIAL";
    private const decimal P_ZodE   = 35m;
    private const string C_ZodE    = "#C96D08";

    private const string L1_Esp    = "SORTEO";
    private const string L2_Esp    = "ESPECIAL";
    private const string N_Esp     = "SORTEO ESPECIAL";
    private const decimal P_Esp    = 60m;
    private const string C_Esp     = "#00A0DE";

    private const string L1_GEsp   = "SORTEO GRAN";
    private const string L2_GEsp   = "ESPECIAL";
    private const string N_GEsp    = "SORTEO GRAN ESPECIAL";
    private const decimal P_GEsp   = 250m;
    private const string C_GEsp    = "#1565C0";

    private const string L1_Mag    = "SORTEO";
    private const string L2_Mag    = "MAGNO";
    private const string N_Mag     = "SORTEO MAGNO";
    private const decimal P_Mag    = 120m;
    private const string C_Mag     = "#C62828";

    private const string L1_Gord   = "SORTEO GORDITO";
    private const string L2_Gord   = "NAVIDEÑO";
    private const string N_Gord    = "SORTEO GORDITO NAVIDEÑO";
    private const decimal P_Gord   = 120m;
    private const string C_Gord    = "#6A1B9A";

    // ── Helpers por tipo ───────────────────────────────────────────────
    private static SorteoActivoLotenal Mayor(int id, string num, string fecha) =>
        new() { IdSorteo = id, NumeroSorteo = num, FechaCelebracion = DateOnly.Parse(fecha),
                NombreSorteo = N_Mayor, Linea1 = L1_Mayor, Linea2 = L2_Mayor, Precio = P_Mayor, ColorHex = C_Mayor };

    private static SorteoActivoLotenal Superior(int id, string num, string fecha) =>
        new() { IdSorteo = id, NumeroSorteo = num, FechaCelebracion = DateOnly.Parse(fecha),
                NombreSorteo = N_Sup, Linea1 = L1_Sup, Linea2 = L2_Sup, Precio = P_Sup, ColorHex = C_Sup };

    private static SorteoActivoLotenal Zodiaco(int id, string num, string fecha) =>
        new() { IdSorteo = id, NumeroSorteo = num, FechaCelebracion = DateOnly.Parse(fecha),
                NombreSorteo = N_Zod, Linea1 = L1_Zod, Linea2 = L2_Zod, Precio = P_Zod, ColorHex = C_Zod };

    private static SorteoActivoLotenal ZodiacoEsp(int id, string num, string fecha) =>
        new() { IdSorteo = id, NumeroSorteo = num, FechaCelebracion = DateOnly.Parse(fecha),
                NombreSorteo = N_ZodE, Linea1 = L1_ZodE, Linea2 = L2_ZodE, Precio = P_ZodE, ColorHex = C_ZodE };

    private static SorteoActivoLotenal Especial(int id, string num, string fecha) =>
        new() { IdSorteo = id, NumeroSorteo = num, FechaCelebracion = DateOnly.Parse(fecha),
                NombreSorteo = N_Esp, Linea1 = L1_Esp, Linea2 = L2_Esp, Precio = P_Esp, ColorHex = C_Esp };

    private static SorteoActivoLotenal GranEspecial(int id, string num, string fecha) =>
        new() { IdSorteo = id, NumeroSorteo = num, FechaCelebracion = DateOnly.Parse(fecha),
                NombreSorteo = N_GEsp, Linea1 = L1_GEsp, Linea2 = L2_GEsp, Precio = P_GEsp, ColorHex = C_GEsp };

    private static SorteoActivoLotenal Magno(int id, string num, string fecha) =>
        new() { IdSorteo = id, NumeroSorteo = num, FechaCelebracion = DateOnly.Parse(fecha),
                NombreSorteo = N_Mag, Linea1 = L1_Mag, Linea2 = L2_Mag, Precio = P_Mag, ColorHex = C_Mag };

    private static SorteoActivoLotenal Gordito(int id, string num, string fecha) =>
        new() { IdSorteo = id, NumeroSorteo = num, FechaCelebracion = DateOnly.Parse(fecha),
                NombreSorteo = N_Gord, Linea1 = L1_Gord, Linea2 = L2_Gord, Precio = P_Gord, ColorHex = C_Gord };

    // ── Catálogo principal ─────────────────────────────────────────────
    private static readonly Dictionary<int, List<SorteoActivoLotenal>> _sorteos = new()
    {
        // Tipo 1 — SORTEO MAYOR $30 (oro #FFB600)
        // Frecuencia: cada martes (semanal). idProducto LOTENAL=1, subcodigo=140
        [1] =
        [
            Mayor(101, "4024", "2026-08-11"),
            Mayor(102, "4025", "2026-08-25"),
            Mayor(103, "4026", "2026-09-01"),
            Mayor(104, "4027", "2026-09-08"),
            Mayor(105, "4028", "2026-09-22"),
            Mayor(106, "4029", "2026-09-29"),
            Mayor(107, "4030", "2026-10-06"),
            Mayor(108, "4031", "2026-10-13"),
            Mayor(109, "4032", "2026-10-20"),
            Mayor(110, "4033", "2026-10-27"),
            Mayor(111, "4034", "2026-11-03"),
            Mayor(112, "4035", "2026-11-10"),
            Mayor(113, "4036", "2026-11-17"),
            Mayor(114, "4037", "2026-11-24"),
            Mayor(115, "4038", "2026-12-01"),
            Mayor(116, "4039", "2026-12-08"),
            Mayor(117, "4040", "2026-12-15"),
        ],

        // Tipo 2 — SORTEO SUPERIOR $40 (verde #4D9D2E)
        // Frecuencia: cada viernes (semanal, con saltos por eventos especiales). idProducto=2, subcodigo=261
        [2] =
        [
            Superior(201, "2894", "2026-08-14"),
            Superior(202, "2895", "2026-08-21"),
            Superior(203, "2896", "2026-08-28"),
            Superior(204, "2897", "2026-09-04"),
            Superior(205, "2898", "2026-09-25"),
            Superior(206, "2899", "2026-10-02"),
            Superior(207, "2900", "2026-10-09"),
            Superior(208, "2901", "2026-10-23"),
            Superior(209, "2902", "2026-10-30"),
            Superior(210, "2903", "2026-11-06"),
            Superior(211, "2904", "2026-11-13"),
            Superior(212, "2905", "2026-11-27"),
            Superior(213, "2906", "2026-12-04"),
            Superior(214, "2907", "2026-12-18"),
        ],

        // Tipo 3 — SORTEO ZODIACO $20 (rosa #ED40A9)
        // Frecuencia: cada domingo (excepto cuando toca Zodiaco Especial). idProducto=4, subcodigo=446
        [3] =
        [
            Zodiaco(301, "1757", "2026-08-16"),
            Zodiaco(302, "1759", "2026-08-30"),
            Zodiaco(303, "1760", "2026-09-06"),
            Zodiaco(304, "1761", "2026-09-20"),
            Zodiaco(305, "1763", "2026-10-04"),
            Zodiaco(306, "1764", "2026-10-11"),
            Zodiaco(307, "1765", "2026-10-18"),
            Zodiaco(308, "1767", "2026-11-08"),
            Zodiaco(309, "1768", "2026-11-15"),
            Zodiaco(310, "1769", "2026-11-22"),
        ],

        // Tipo 4 — SORTEO ZODIACO ESPECIAL $35 (naranja #C96D08)
        // Frecuencia: aprox. cada 4 domingos. idProducto=4 (tipo zodiacoe), subcodigo=448
        [4] =
        [
            ZodiacoEsp(401, "1754", "2026-07-26"),
            ZodiacoEsp(402, "1758", "2026-08-23"),
            ZodiacoEsp(403, "1762", "2026-09-27"),
            ZodiacoEsp(404, "1766", "2026-10-25"),
            ZodiacoEsp(405, "1770", "2026-11-29"),
            ZodiacoEsp(406, "1771", "2026-12-06"),
        ],

        // Tipo 5 — SORTEO ESPECIAL $60 (azul #00A0DE)
        // Frecuencia: mensual. idProducto=5, subcodigo=516
        [5] =
        [
            Especial(501, "315", "2026-08-18"),
            Especial(502, "316", "2026-09-18"),
            Especial(503, "317", "2026-10-16"),
            Especial(504, "318", "2026-11-20"),
            Especial(505, "319", "2026-12-12"),
        ],

        // Tipo 6 — SORTEO GRAN ESPECIAL $250 (azul oscuro #1565C0)
        // No aparece en el calendario oficial actual. Datos placeholder.
        [6] =
        [
            GranEspecial(601, "208", "2026-09-19"),
            GranEspecial(602, "209", "2026-10-17"),
        ],

        // Tipo 7 — SORTEO MAGNO $120 (rojo #C62828)
        // Frecuencia: eventos especiales (Sep 16, Dic 31). idProducto=3
        [7] =
        [
            Magno(701, "392", "2026-09-16"),
            Magno(702, "393", "2026-12-31"),
        ],

        // Tipo 8 — SORTEO GORDITO NAVIDEÑO $120 (morado #6A1B9A)
        // Frecuencia: navideña (Dic 24). idProducto=6
        [8] =
        [
            Gordito(801, "225", "2026-12-24"),
        ],
    };
}
