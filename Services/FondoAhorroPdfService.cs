using System.Globalization;
using SkiaSharp;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Genera el PDF "Reporte de Fondo de Ahorro" (mockup 9.2 contenido
/// pdf) con SkiaSharp, siguiendo el patrón de EstadoDeCuentaPdfService:
/// página carta 612×792, logo, título + subtítulo con el periodo real,
/// titular, tabla de movimientos con encabezado rojo #F44336
/// (Fecha | Folio | Origen | Monto, VACÍA en esta fase), barra roja de
/// Total y pie institucional #E53935.
/// </summary>
public class FondoAhorroPdfService
{
    /// <summary>Ancho carta en puntos.</summary>
    private const float Ancho = 612f;

    /// <summary>Alto carta en puntos.</summary>
    private const float Alto = 792f;

    /// <summary>Alto de línea de texto normal (pt).</summary>
    private const float Linea = 13.5f;

    /// <summary>Rojo de los encabezados de tabla (#F44336, como Estado de Cuenta).</summary>
    private static readonly SKColor RojoTabla = new(0xF4, 0x43, 0x36);

    /// <summary>Rojo del pie de página y de la barra de Total (#E53935).</summary>
    private static readonly SKColor RojoPie = new(0xE5, 0x39, 0x35);

    /// <summary>Genera el PDF del Fondo de Ahorro y devuelve su ruta absoluta.</summary>
    public Task<string> GenerarAsync(FondoAhorro fondo)
    {
        string nombre = $"fondo-ahorro-{fondo.FechaFin:dd}-{MesCorto(fondo.FechaFin)}-{fondo.FechaFin:yyyy}.pdf";
        string dir = FileSystem.AppDataDirectory;
        string ruta = Path.Combine(dir, nombre);

        using var archivo = new SKFileWStream(ruta);
        using var documento = SKDocument.CreatePdf(archivo);
        using SKCanvas canvas = documento.BeginPage(Ancho, Alto);

        using var fuenteNormal = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 9.5f,
            IsAntialias = true,
        };
        using var fuenteNegrita = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 9.5f,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(null, SKTypefaceStyle.Bold),
        };
        using var fuenteTitulo = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14f,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(null, SKTypefaceStyle.Bold),
        };
        using var fuenteBlanca = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 8.5f,
            IsAntialias = true,
        };
        using var fuenteBlancaNegrita = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 8.5f,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(null, SKTypefaceStyle.Bold),
        };

        float y = 20f;

        // ── Logo Mi Cachito ───────────────────────────────────────────
        using SKBitmap? logo = CargarLogo();
        if (logo is not null)
        {
            float escala = 70f / logo.Height;
            float w = logo.Width * escala;
            canvas.DrawBitmap(logo, (Ancho - w) / 2f, y, new SKPaint { FilterQuality = SKFilterQuality.Medium });
            y += 70f + 4f;
        }

        // ── Título + subtítulo con el periodo real ────────────────────
        DibujarCentrado(canvas, fuenteTitulo, "Reporte de Fondo de Ahorro", y);
        y += 18f;
        string desde = FormatoFecha(fondo.FechaInicio);
        string hasta = FormatoFecha(fondo.FechaFin);
        DibujarCentrado(canvas, fuenteNegrita, $"Del {desde} al {hasta}", y);
        y += 22f;

        // ── Datos del titular ─────────────────────────────────────────
        DibujarTexto(canvas, fuenteNegrita, $"Titular: {Vacio(fondo.Titular)}", 34f, y);
        y += Linea + 12f;

        // ── Tabla de movimientos (encabezado rojo, 4 columnas) ───────
        y = TablaMovimientos(canvas, fondo, y, fuenteNormal, fuenteBlancaNegrita);

        // ── Barra roja de Total ───────────────────────────────────────
        y += 14f;
        using var barraTotal = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 20f), barraTotal);
        DibujarTexto(canvas, fuenteBlancaNegrita, "Total:", 40f, y + 14f);
        DibujarDerecha(canvas, fuenteBlancaNegrita, Moneda(fondo.Total), Ancho - 40f, y + 14f);

        // ── Pie con fondo rojo (mismo institucional) ──────────────────
        using var fondoPie = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(0f, Alto - 74f, Ancho, 74f), fondoPie);

        float yPie = Alto - 48f;
        DibujarCentrado(canvas, fuenteBlancaNegrita, "Comercializadora de la Suerte S.A de C.V", yPie);
        yPie += Linea;
        DibujarCentrado(canvas, fuenteBlanca, "Calle Segunda Ote. Sur, Col. Centro, C.P. 29000, Tuxtla Gutiérrez, Chis.", yPie);
        yPie += Linea;
        DibujarCentrado(canvas, fuenteBlanca, "Teléfono: (961) 6120235", yPie);

        documento.EndPage();
        documento.Close();

        return Task.FromResult(ruta);
    }

    /// <summary>
    /// Dibuja la tabla de movimientos: barra de encabezado roja con las
    /// 4 columnas (Fecha, Folio, Origen, Monto) y las filas. En esta
    /// fase la tabla está VACÍA (sin registros) — solo el encabezado.
    /// </summary>
    private static float TablaMovimientos(
        SKCanvas canvas,
        FondoAhorro fondo,
        float y,
        SKPaint fuenteNormal,
        SKPaint fuenteBlancaNegrita)
    {
        using var barra = new SKPaint { Color = RojoTabla, Style = SKPaintStyle.Fill };
        using var borde = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), barra);
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), borde);

        // Columnas: Fecha | Folio | Origen | Monto
        float[] xs = { 36f, 200f, 350f, Ancho - 36f };
        string[] titulos = { "Fecha", "Folio", "Origen", "Monto" };
        for (int i = 0; i < titulos.Length; i++)
        {
            if (i == titulos.Length - 1)
            {
                DibujarDerecha(canvas, fuenteBlancaNegrita, titulos[i], xs[i], y + 11.5f);
            }
            else
            {
                DibujarTexto(canvas, fuenteBlancaNegrita, titulos[i], xs[i], y + 11.5f);
            }
        }

        y += 16f + 8f;

        // Filas de movimientos (VACÍA en esta fase)
        foreach (MovimientoFondoAhorro m in fondo.Movimientos)
        {
            DibujarTexto(canvas, fuenteNormal, m.Fecha.ToString("dd-MM-yyyy", CultureInfo.CurrentCulture), xs[0], y);
            DibujarTexto(canvas, fuenteNormal, m.Folio, xs[1], y);
            DibujarTexto(canvas, fuenteNormal, m.Origen, xs[2], y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(m.Monto), xs[3], y);
            y += Linea;
        }

        return y;
    }

    /// <summary>Fecha en formato de pantalla: "29-octubre-2025".</summary>
    private static string FormatoFecha(DateTime d) =>
        $"{d:dd}-{d.ToString("MMMM", CultureInfo.CreateSpecificCulture("es-MX")).ToLowerInvariant()}-{d:yyyy}";

    /// <summary>Mes corto para el nombre del archivo: "octubre" → "octubre".</summary>
    private static string MesCorto(DateTime d) =>
        d.ToString("MMMM", CultureInfo.CreateSpecificCulture("es-MX")).ToLowerInvariant();

    /// <summary>Texto o guion si está vacío (estado vacío, sin inventar datos).</summary>
    private static string Vacio(string texto) => string.IsNullOrWhiteSpace(texto) ? "—" : texto;

    /// <summary>Decimal formateado como moneda: "$ 0.00".</summary>
    private static string Moneda(decimal v) =>
        $"$ {v.ToString("#,##0.00", CultureInfo.CurrentCulture)}";

    // ── Helpers de dibujo (mismos de EstadoDeCuentaPdfService) ──────

    private static SKBitmap? CargarLogo()
    {
        try
        {
            using Stream stream = FileSystem.OpenAppPackageFileAsync(
                "logo_ticket_pdf.png").GetAwaiter().GetResult();
            return SKBitmap.Decode(stream);
        }
        catch
        {
            return null;
        }
    }

    private static void DibujarTexto(SKCanvas canvas, SKPaint fuente, string texto, float x, float y)
        => canvas.DrawText(texto, x, y, fuente);

    private static void DibujarDerecha(SKCanvas canvas, SKPaint fuente, string texto, float xDerecha, float y)
    {
        float ancho = fuente.MeasureText(texto);
        canvas.DrawText(texto, xDerecha - ancho, y, fuente);
    }

    private static void DibujarCentrado(SKCanvas canvas, SKPaint fuente, string texto, float y)
    {
        float ancho = fuente.MeasureText(texto);
        canvas.DrawText(texto, (Ancho - ancho) / 2f, y, fuente);
    }
}
