using System.Globalization;
using SkiaSharp;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Genera el PDF "Estado de Cuenta Informativo" (mockup 9.1 contenido
/// pdf) con SkiaSharp 1.57, reutilizando estructura/helpers de
/// ReciboPagoPdfService: página carta 612×792, logo Mi Cachito,
/// título, datos del vendedor a la izquierda y tabla resumen a la
/// derecha (todo $0.00), tabla principal de sorteos con encabezado
/// rojo #F44336 (7 columnas, VACÍA en esta fase), totales y pie con
/// fondo rojo #E53935 institucional.
/// </summary>
public class EstadoDeCuentaPdfService
{
    /// <summary>Ancho carta en puntos.</summary>
    private const float Ancho = 612f;

    /// <summary>Alto carta en puntos.</summary>
    private const float Alto = 792f;

    /// <summary>Alto de línea de texto normal (pt).</summary>
    private const float Linea = 13.5f;

    /// <summary>Rojo de los encabezados de tabla (#F44336, medido en el PDF ref).</summary>
    private static readonly SKColor RojoTabla = new(0xF4, 0x43, 0x36);

    /// <summary>Rojo del pie de página (#E53935).</summary>
    private static readonly SKColor RojoPie = new(0xE5, 0x39, 0x35);

    /// <summary>Genera el PDF del Estado de Cuenta y devuelve su ruta absoluta.</summary>
    public Task<string> GenerarAsync(EstadoCuenta estado)
    {
        string nombre = $"estado-cuenta-{estado.FechaEmision:dd-MM-yyyy}.pdf";
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

        // ── Título ────────────────────────────────────────────────────
        DibujarCentrado(canvas, fuenteTitulo, "Estado de Cuenta Informativo", y);
        y += 20f;

        // ── Datos del vendedor (izquierda) + resumen (derecha) ────────
        float yIzq = y;
        DibujarTexto(canvas, fuenteNegrita, $"Fecha de Emisión: {estado.FechaEmision:dd-MM-yyyy}", 34f, yIzq);
        yIzq += Linea;
        DibujarTexto(canvas, fuenteNormal, $"Vendedor: {estado.Vendedor}", 34f, yIzq);
        yIzq += Linea;
        DibujarTexto(canvas, fuenteNormal, $"Plaza: {estado.Plaza}", 34f, yIzq);
        yIzq += Linea;

        // Tabla resumen a la derecha (valores en cero en esta fase)
        float xResumen = 330f;
        float yRes = y;
        DibujarTexto(canvas, fuenteNegrita, "Concepto", xResumen, yRes);
        DibujarDerecha(canvas, fuenteNegrita, "Monto", Ancho - 36f, yRes);
        yRes += Linea + 2f;
        foreach ((string etiqueta, decimal monto) in ResumenFilas(estado))
        {
            DibujarTexto(canvas, fuenteNormal, etiqueta, xResumen, yRes);
            DibujarDerecha(canvas, fuenteNormal, Moneda(monto), Ancho - 36f, yRes);
            yRes += Linea;
        }

        y = Math.Max(yIzq, yRes) + 12f;

        // ── Tabla principal de sorteos (encabezado rojo, 7 columnas) ──
        y = TablaSorteos(canvas, estado, y, fuenteNormal, fuenteNegrita, fuenteBlancaNegrita);

        // ── Totales generales ─────────────────────────────────────────
        y += 4f;
        DibujarDerecha(canvas, fuenteNegrita, $"Vencido: {Moneda(estado.VencidoTotal)}", Ancho - 36f, y);
        y += Linea;
        DibujarDerecha(canvas, fuenteNegrita, $"Consigna: {Moneda(estado.ConsignaTotal)}", Ancho - 36f, y);
        y += Linea;
        DibujarDerecha(canvas, fuenteNegrita, $"Pagos: {Moneda(estado.PagosTotal)}", Ancho - 36f, y);
        y += Linea;
        DibujarDerecha(canvas, fuenteNegrita, $"Total: {Moneda(estado.TotalFinal)}", Ancho - 36f, y);

        // ── Pie con fondo rojo (mismo institucional de Recibos) ───────
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

    /// <summary>Filas del bloque resumen (izquierda del PDF y pantalla).</summary>
    private static IReadOnlyList<(string Etiqueta, decimal Monto)> ResumenFilas(EstadoCuenta estado) => new List<(string, decimal)>
    {
        ("Fondo de Ahorro", estado.FondoDeAhorro),
        ("Fideicomiso", estado.Fideicomiso),
        ("Pagarés", estado.Pagares),
        ("Bolsa Electrónica", estado.BolsaElectronica),
        ("Garantía Total", estado.GarantiaTotal),
        ("Capacidad de Crédito", estado.CapacidadDeCredito),
    };

    /// <summary>
    /// Dibuja la tabla principal: barra de encabezado roja con las 7
    /// columnas (Sorteo, Fecha, Cantidad, Vencido, Consigna, Pagos,
    /// Total) y las filas de sorteos. En esta fase la tabla está VACÍA
    /// (sin registros) — se dibuja solo el encabezado.
    /// </summary>
    private static float TablaSorteos(
        SKCanvas canvas,
        EstadoCuenta estado,
        float y,
        SKPaint fuenteNormal,
        SKPaint fuenteNegrita,
        SKPaint fuenteBlancaNegrita)
    {
        // Barra de encabezado roja
        using var barra = new SKPaint { Color = RojoTabla, Style = SKPaintStyle.Fill };
        using var borde = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), barra);
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), borde);

        // Columnas: Sorteo | Fecha | Cantidad | Vencido | Consigna | Pagos | Total
        float[] xs = { 36f, 250f, 320f, 380f, 440f, 500f, Ancho - 36f };
        string[] titulos = { "Sorteo", "Fecha", "Cantidad", "Vencido", "Consigna", "Pagos", "Total" };
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

        // Filas de sorteos (VACÍA en esta fase)
        foreach (SorteoEstadoCuenta s in estado.Sorteos)
        {
            DibujarTexto(canvas, fuenteNegrita, s.Sorteo, xs[0], y);
            DibujarTexto(canvas, fuenteNormal, s.Fecha.ToString("dd-MM-yyyy", CultureInfo.CurrentCulture), xs[1], y);
            DibujarDerecha(canvas, fuenteNormal, s.Cantidad.ToString(CultureInfo.CurrentCulture), xs[2] + 30f, y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(s.Vencido), xs[3] + 40f, y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(s.Consigna), xs[4] + 40f, y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(s.Pagos), xs[5] + 40f, y);
            DibujarDerecha(canvas, fuenteNegrita, Moneda(s.Total), xs[6], y);
            y += Linea;
        }

        return y;
    }

    /// <summary>Decimal formateado como moneda: "$ 0.00".</summary>
    private static string Moneda(decimal v) =>
        $"$ {v.ToString("#,##0.00", CultureInfo.CurrentCulture)}";

    // ── Helpers de dibujo (mismos de ReciboPagoPdfService) ──────────

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
