using System.Globalization;
using SkiaSharp;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Genera el PDF "Reporte de Facturación" (mockup 9.3 contenido pdf)
/// con SkiaSharp, patrón de FondoAhorroPdfService: página carta
/// 612×792, logo, título + subtítulo con el periodo real, Vendedor,
/// una sección por categoría (barra roja con el nombre + encabezado
/// rojo Fecha|Sorteo|Entrega|Devolución|Venta|Ganancia + filas +
/// subtotal dinámico), barra roja de Total general y pie institucional
/// con Fecha de Impresión (momento real de la generación).
/// </summary>
public class FacturacionPdfService
{
    /// <summary>Ancho carta en puntos.</summary>
    private const float Ancho = 612f;

    /// <summary>Alto carta en puntos.</summary>
    private const float Alto = 792f;

    /// <summary>Alto de línea de texto normal (pt).</summary>
    private const float Linea = 13.5f;

    /// <summary>Rojo de las barras de encabezado (#F44336, como los demás PDF).</summary>
    private static readonly SKColor RojoTabla = new(0xF4, 0x43, 0x36);

    /// <summary>Rojo del pie y de las barras de subtotal/total (#E53935).</summary>
    private static readonly SKColor RojoPie = new(0xE5, 0x39, 0x35);

    /// <summary>Genera el PDF de Facturación y devuelve su ruta absoluta.</summary>
    public Task<string> GenerarAsync(Facturacion reporte)
    {
        string nombre = $"facturacion-{reporte.FechaFin:dd}-{MesEspanol(reporte.FechaFin)}-{reporte.FechaFin:yyyy}.pdf";
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
        DibujarCentrado(canvas, fuenteTitulo, "Reporte de Facturación", y);
        y += 18f;
        string desde = FormatoFecha(reporte.FechaInicio);
        string hasta = FormatoFecha(reporte.FechaFin);
        DibujarCentrado(canvas, fuenteNegrita, $"Del {desde} al {hasta}", y);
        y += 22f;

        // ── Datos del vendedor ────────────────────────────────────────
        DibujarTexto(canvas, fuenteNegrita, $"Vendedor: {Vacio(reporte.Vendedor)}", 34f, y);
        y += Linea + 12f;

        // ── Sección por categoría (dinámica según los registros) ─────
        foreach (CategoriaFacturacion categoria in reporte.Categorias)
        {
            // Barra de título de la categoría
            using var barraCategoria = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
            canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), barraCategoria);
            DibujarTexto(canvas, fuenteBlancaNegrita, categoria.Nombre, 40f, y + 11.5f);
            y += 16f + 6f;

            // Encabezado de columnas
            y = TablaCategoria(canvas, reporte, categoria.Nombre, y, fuenteNormal, fuenteBlancaNegrita);
            if (y > Alto - 140f)
            {
                // Los datos reales paginarán; en esta fase las 2 filas caben.
                break;
            }

            // Subtotal de la categoría (dinámico)
            y += 6f;
            using var barraSubtotal = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
            canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), barraSubtotal);
            DibujarDerecha(canvas, fuenteBlancaNegrita, $"Subtotal {categoria.Nombre}: {Moneda(categoria.Monto)}", Ancho - 40f, y + 11.5f);
            y += 16f + 12f;
        }

        // ── Barra roja de Total general (dinámico) ────────────────────
        using var barraTotal = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 20f), barraTotal);
        DibujarTexto(canvas, fuenteBlancaNegrita, "Total:", 40f, y + 14f);
        DibujarDerecha(canvas, fuenteBlancaNegrita, Moneda(reporte.TotalFacturado), Ancho - 40f, y + 14f);

        // ── Pie con fondo rojo + Fecha de Impresión ───────────────────
        using var fondoPie = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(0f, Alto - 74f, Ancho, 74f), fondoPie);

        float yPie = Alto - 48f;
        DibujarCentrado(canvas, fuenteBlancaNegrita, "Comercializadora de la Suerte S.A de C.V", yPie);
        yPie += Linea;
        DibujarCentrado(canvas, fuenteBlanca, "Calle Segunda Ote. Sur, Col. Centro, C.P. 29000, Tuxtla Gutiérrez, Chis.", yPie);
        yPie += Linea;
        DibujarCentrado(canvas, fuenteBlanca, "Teléfono: (961) 6120235", yPie);
        yPie += Linea + 2f;
        DibujarCentrado(canvas, fuenteBlanca, $"Fecha de Impresión: {DateTime.Now:dd-MM-yyyy HH:mm}", yPie);

        documento.EndPage();
        documento.Close();

        return Task.FromResult(ruta);
    }

    /// <summary>
    /// Dibuja el encabezado rojo de columnas y las filas de la categoría.
    /// </summary>
    private static float TablaCategoria(
        SKCanvas canvas,
        Facturacion reporte,
        string categoria,
        float y,
        SKPaint fuenteNormal,
        SKPaint fuenteBlancaNegrita)
    {
        using var barra = new SKPaint { Color = RojoTabla, Style = SKPaintStyle.Fill };
        using var borde = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), barra);
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), borde);

        // Columnas: Fecha | Sorteo | Entrega | Devolución | Venta | Ganancia
        float[] xs = { 36f, 140f, 240f, 330f, 430f, Ancho - 36f };
        string[] titulos = { "Fecha", "Sorteo", "Entrega", "Devolución", "Venta", "Ganancia" };
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

        y += 16f + 6f;

        foreach (RegistroFacturacion r in reporte.Registros.Where(r => r.Sorteo == categoria))
        {
            DibujarTexto(canvas, fuenteNormal, r.Fecha.ToString("dd-MM-yyyy", CultureInfo.CurrentCulture), xs[0], y);
            DibujarTexto(canvas, fuenteNormal, r.Sorteo, xs[1], y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(r.Entrega), xs[2] + 55f, y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(r.Devolucion), xs[3] + 55f, y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(r.Venta), xs[4] + 45f, y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(r.Ganancia), xs[5], y);
            y += Linea;
        }

        return y;
    }

    /// <summary>Fecha en formato de pantalla: "09-septiembre-2026".</summary>
    private static string FormatoFecha(DateTime d) =>
        $"{d:dd}-{d.ToString("MMMM", CultureInfo.CreateSpecificCulture("es-MX")).ToLowerInvariant()}-{d:yyyy}";

    /// <summary>Mes en español minúscula ("septiembre").</summary>
    private static string MesEspanol(DateTime d) =>
        d.ToString("MMMM", System.Globalization.CultureInfo.CreateSpecificCulture("es-MX")).ToLowerInvariant();

    /// <summary>Texto o guion si está vacío (estado vacío, sin inventar datos).</summary>
    private static string Vacio(string texto) => string.IsNullOrWhiteSpace(texto) ? "—" : texto;

    /// <summary>Decimal formateado como moneda: "$ 1,235.50".</summary>
    private static string Moneda(decimal v) =>
        $"$ {v.ToString("#,##0.00", CultureInfo.CurrentCulture)}";

    // ── Helpers de dibujo (mismos de FondoAhorroPdfService) ─────────

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
