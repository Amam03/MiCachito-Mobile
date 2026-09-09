using SkiaSharp;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Genera el PDF "Comprobante de Pago" (mockup 8.3) con SkiaSharp 1.57
/// (ya presente vía ZXing). Replica el PDF de referencia: página carta
/// (612×792 pt), logo Mi Cachito, leyendas LOTERÍA NACIONAL para la
/// Asistencia Pública, Agencia Expendedora, RECIBO DE CAJA con
/// FECHA/FOLIO/(CEDIS), Cliente, tabla Documentos Pagados con encabezado
/// rojo, Total Documentos, tabla Formas de Pago con encabezado rojo,
/// Total, y pie con fondo rojo (razón social, dirección y teléfono).
/// Fase SOLO INTERFAZ: datos del recibo en memoria.
/// </summary>
public class ReciboPagoPdfService
{
    /// <summary>Alto de línea de texto normal (pt).</summary>
    private const float Linea = 14.5f;

    /// <summary>Ancho carta en puntos.</summary>
    private const float Ancho = 612f;

    /// <summary>Rojo de los encabezados de tabla (#F44336, medido en el PDF ref).</summary>
    private static readonly SKColor RojoTabla = new(0xF4, 0x43, 0x36);

    /// <summary>Rojo del pie de página (#E53935, medido en el PDF ref).</summary>
    private static readonly SKColor RojoPie = new(0xE5, 0x39, 0x35);

    /// <summary>Genera el PDF del recibo y devuelve su ruta absoluta.</summary>
    public Task<string> GenerarAsync(ReciboPago recibo)
    {
        string dir = FileSystem.AppDataDirectory;
        string ruta = Path.Combine(dir, recibo.NombreArchivoPdf + ".pdf");

        using var archivo = new SKFileWStream(ruta);
        using var documento = SKDocument.CreatePdf(archivo);
        using SKCanvas canvas = documento.BeginPage(Ancho, 792f);

        using var fuenteNormal = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 10.8f,
            IsAntialias = true,
        };
        using var fuenteNegrita = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 10.8f,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(null, SKTypefaceStyle.Bold),
        };
        using var fuenteTitulo = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 15f,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(null, SKTypefaceStyle.Bold),
        };
        using var fuenteBlanca = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 10.8f,
            IsAntialias = true,
        };
        using var fuenteBlancaNegrita = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 10.8f,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(null, SKTypefaceStyle.Bold),
        };

        float y = 20f;

        // ── Logo Mi Cachito ───────────────────────────────────────────
        using SKBitmap? logo = CargarLogo();
        if (logo is not null)
        {
            float escala = 84f / logo.Height;
            float w = logo.Width * escala;
            canvas.DrawBitmap(logo, (Ancho - w) / 2f, y, new SKPaint { FilterQuality = SKFilterQuality.Medium });
            y += 84f + 6f;
        }

        // ── Leyendas institucionales ──────────────────────────────────
        DibujarCentrado(canvas, fuenteTitulo, "LOTERÍA NACIONAL", y);
        y += 18f;
        DibujarCentrado(canvas, fuenteNormal, "para la Asistencia Pública", y);
        y += Linea;
        DibujarCentrado(canvas, fuenteNormal, "Agencia Expendedora de Primera en Tuxtla Gutiérrez, Chiapas", y);
        y += Linea + 4f;

        // ── RECIBO DE CAJA FECHA ... FOLIO ... (CEDIS ...) ────────────
        DibujarCentrado(
            canvas,
            fuenteNegrita,
            $"RECIBO DE CAJA FECHA {recibo.FechaPdf} FOLIO {recibo.Folio} ({recibo.Cedis})",
            y);
        y += Linea + 2f;

        // ── Cliente ───────────────────────────────────────────────────
        DibujarCentrado(canvas, fuenteNormal, $"Cliente: {recibo.Cliente}", y);
        y += Linea + 14f;

        // ── Tabla 1: Documentos Pagados ───────────────────────────────
        y = TablaConEncabezadoRojo(
            canvas,
            titulo: "Documentos Pagados",
            filas: recibo.DocumentosPagados.Select(d => (d.Referencia, d.MontoAbsTexto)).ToList(),
            totalEtiqueta: "Total Documentos:",
            totalTexto: recibo.TotalDocumentos.FormatoMoneda(),
            y: y,
            fuenteNormal,
            fuenteNegrita,
            fuenteBlanca,
            fuenteBlancaNegrita);

        y += 14f;

        // ── Tabla 2: Formas de Pago ───────────────────────────────────
        y = TablaConEncabezadoRojo(
            canvas,
            titulo: "Formas de Pago",
            filas: recibo.FormasPago.Select(f => (f.Forma, f.Monto.FormatoMoneda())).ToList(),
            totalEtiqueta: "Total:",
            totalTexto: recibo.Total.FormatoMoneda(),
            y: y,
            fuenteNormal,
            fuenteNegrita,
            fuenteBlanca,
            fuenteBlancaNegrita);

        // ── Pie con fondo rojo (3 líneas, ancho completo) ─────────────
        // El PDF de referencia coloca el pie pegado al borde inferior.
        using var fondoPie = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(0f, 792f - 74f, Ancho, 74f), fondoPie);

        float yPie = 792f - 48f;
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
    /// Dibuja una tabla del comprobante: barra de título roja con texto
    /// blanco (encabezado), filas etiqueta/valor y total en negrita.
    /// Devuelve la y final.
    /// </summary>
    private static float TablaConEncabezadoRojo(
        SKCanvas canvas,
        string titulo,
        IReadOnlyList<(string Etiqueta, string Valor)> filas,
        string totalEtiqueta,
        string totalTexto,
        float y,
        SKPaint fuenteNormal,
        SKPaint fuenteNegrita,
        SKPaint fuenteBlanca,
        SKPaint fuenteBlancaNegrita)
    {
        // Barra de título roja (borde 1pt negro como el ref, texto blanco)
        using var barra = new SKPaint { Color = RojoTabla, Style = SKPaintStyle.Fill };
        using var borde = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 17f), barra);
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 17f), borde);

        DibujarTexto(canvas, fuenteBlancaNegrita, titulo, 36f, y + 12.5f);
        DibujarDerecha(canvas, fuenteBlancaNegrita, "Monto", Ancho - 36f, y + 12.5f);
        y += 17f + 8f;

        foreach ((string etiqueta, string valor) in filas)
        {
            DibujarTexto(canvas, fuenteNormal, etiqueta, 36f, y);
            DibujarDerecha(canvas, fuenteNormal, valor, Ancho - 36f, y);
            y += Linea;
        }

        y += 2f;
        DibujarDerecha(canvas, fuenteNegrita, $"{totalEtiqueta} {totalTexto}", Ancho - 36f, y);
        return y + Linea;
    }

    /// <summary>Decimal formateado como moneda: "$ 6,170.00".</summary>

    // ── Helpers de dibujo (mismos de TicketPdfService) ────────────────

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

/// <summary>Extensiones de formato para el comprobante.</summary>
internal static class FormatoMonedaExtensions
{
    /// <summary>Decimal → "$ 6,170.00" (formato del PDF de referencia).</summary>
    public static string FormatoMoneda(this decimal v) =>
        $"$ {v.ToString("#,##0.00", System.Globalization.CultureInfo.CurrentCulture)}";
}
