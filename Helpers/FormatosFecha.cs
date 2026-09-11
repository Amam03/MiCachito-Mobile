using System.Globalization;

namespace MiCachito.Mobile.Helpers;

/// <summary>
/// Formato de fechas en español (es-MX) para toda la app.
/// NUNCA usar CurrentCulture para meses por nombre: el dispositivo puede
/// correr en en-US y renderizar "28-October-2025" (bug real Recibos de
/// Pago, hallado en la auditoría 2026-09-10).
/// </summary>
public static class FormatosFecha
{
    private static readonly CultureInfo Cultura = CultureInfo.CreateSpecificCulture("es-MX");

    /// <summary>Mes en español, minúscula: "octubre".</summary>
    public static string MesEspanol(DateTime d) =>
        d.ToString("MMMM", Cultura).ToLowerInvariant();

    /// <summary>Fecha de nombre de archivo: "09-octubre-2026".</summary>
    public static string FechaArchivo(DateTime d) =>
        $"{d:dd}-{MesEspanol(d)}-{d:yyyy}";

    /// <summary>Fecha larga para pantalla: "28-octubre-2025".</summary>
    public static string FechaLarga(DateTime d) => FechaArchivo(d);

    /// <summary>Fecha larga para pantalla (DateOnly): "28-octubre-2025".</summary>
    public static string FechaLarga(DateOnly d) =>
        $"{d:dd}-{d.ToString("MMMM", Cultura).ToLowerInvariant()}-{d:yyyy}";

    /// <summary>Fecha y hora para pantalla: "28-octubre-2025 09:30" (12 h).</summary>
    public static string FechaHoraLarga(DateTime fecha, TimeSpan hora) =>
        $"{FechaLarga(fecha)} {hora:hh\\:mm}";
}
