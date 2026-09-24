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
        // FASE 3: el PDF pagina con los datos reales — el canvas se renueva
        // por página (BeginPage) y NO se dispone con using (cada página la
        // destruye EndPage; el documento se cierra con Close).
        SKCanvas canvas = documento.BeginPage(Ancho, Alto);

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

        // ── Datos del vendedor (FASE 3: reales del endpoint) ──────────
        DibujarTexto(canvas, fuenteNegrita, $"Vendedor: {Vacio(reporte.Vendedor)}", 34f, y);
        y += Linea;
        DibujarTexto(canvas, fuenteNormal, $"Comisión: {reporte.ComisionPct:0.##}%", 34f, y);
        y += Linea + 12f;

        // ── Sección por categoría (dinámica; pagina con datos reales) ─
        foreach (CategoriaFacturacion categoria in reporte.Categorias)
        {
            // Alto estimado de la sección completa: barra + encabezado +
            // filas + subtotal — si no cabe, página nueva ANTES de empezar.
            int filas = reporte.Registros.Count(r => r.Categoria == categoria.Nombre);
            float altoSeccion = 16f + 6f + 16f + 6f + (filas * Linea) + 6f + 16f + 12f;
            if (y + altoSeccion > Alto - 140f)
            {
                NuevaPagina(documento, ref canvas, ref y);
            }

            // Barra de título de la categoría
            using var barraCategoria = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
            canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), barraCategoria);
            DibujarTexto(canvas, fuenteBlancaNegrita, categoria.Nombre, 40f, y + 11.5f);
            y += 16f + 6f;

            // Encabezado + filas (página nueva por fila si la categoría es larga)
            y = TablaCategoria(documento, ref canvas, reporte, categoria.Nombre, y, fuenteNormal, fuenteBlancaNegrita);

            // Subtotal de la categoría (dinámico)
            y += 6f;
            if (y + 16f > Alto - 140f)
            {
                NuevaPagina(documento, ref canvas, ref y);
            }
            using var barraSubtotal = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
            canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), barraSubtotal);
            DibujarDerecha(canvas, fuenteBlancaNegrita, $"Subtotal {categoria.Nombre}: {Moneda(categoria.Monto)}", Ancho - 40f, y + 11.5f);
            y += 16f + 12f;
        }

        // ── Barra roja de Total general (dinámico; respeta el pie) ─────
        if (y + 20f > Alto - 140f)
        {
            NuevaPagina(documento, ref canvas, ref y);
        }
        using var barraTotal = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 20f), barraTotal);
        DibujarTexto(canvas, fuenteBlancaNegrita, "Total:", 40f, y + 14f);
        DibujarDerecha(canvas, fuenteBlancaNegrita, Moneda(reporte.TotalFacturado), Ancho - 40f, y + 14f);

        // ── Pie con fondo rojo + Fecha de Impresión (página final) ────
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
    /// Dibuja el encabezado rojo de columnas y las filas de la categoría
    /// (FASE 3: paginación por fila con los datos reales).
    /// </summary>
    private static float TablaCategoria(
        SKDocument documento,
        ref SKCanvas canvas,
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

        foreach (RegistroFacturacion r in reporte.Registros.Where(r => r.Categoria == categoria))
        {
            if (y + Linea > Alto - 140f)
            {
                NuevaPagina(documento, ref canvas, ref y);
            }
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

    /// <summary>
    /// FASE 3: cierra la página actual y abre una nueva con el margen
    /// superior estándar (el pie institucional se dibuja solo en la
    /// página final; las intermedias quedan limpias).
    /// </summary>
    private static void NuevaPagina(SKDocument documento, ref SKCanvas canvas, ref float y)
    {
        documento.EndPage();
        canvas = documento.BeginPage(Ancho, Alto);
        y = 20f;
    }

    /// <summary>Fecha en formato de pantalla: "09-septiembre-2026".</summary>
    private static string FormatoFecha(DateTime d) =>
        Helpers.FormatosFecha.FechaArchivo(d);

    /// <summary>Mes en español minúscula ("septiembre").</summary>
    private static string MesEspanol(DateTime d) =>
        Helpers.FormatosFecha.MesEspanol(d);

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
