using System.Globalization;
using SkiaSharp;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Genera el PDF "Estado de Cuenta Informativo" (mockup 9.1 contenido
/// pdf) con SkiaSharp 1.57, reutilizando estructura/helpers de
/// ReciboPagoPdfService: página carta 612×792, logo Mi Cachito, título,
/// datos del vendedor a la izquierda y tabla resumen a la derecha,
/// tabla principal de sorteos con encabezado rojo #F44336 (7 columnas)
/// y PAGINACIÓN real (los sorteos que no caben pasan a página nueva),
/// totales y pie con fondo rojo #E53935 institucional.
/// FASE 4 (2026-09-22): datos REALES del endpoint (Vendedor/Plaza del
/// billetero de la sesión, conceptos y filas solo-consignación).
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
        // El canvas de BeginPage NO se dispone con using (cada EndPage
        // destruye la página; el documento se cierra con Close) — mismo
        // pitfall documentado de FacturacionPdfService.
        SKCanvas canvas = documento.BeginPage(Ancho, Alto);
        float y = 20f;

        using var fuenteNormal = Fuente(SKColors.Black, 9.5f, false);
        using var fuenteNegrita = Fuente(SKColors.Black, 9.5f, true);
        using var fuenteTitulo = Fuente(SKColors.Black, 14f, true);
        using var fuenteBlanca = Fuente(SKColors.White, 8.5f, false);
        using var fuenteBlancaNegrita = Fuente(SKColors.White, 8.5f, true);

        y = DibujarEncabezado(canvas, estado, y, fuenteTitulo, fuenteNegrita, fuenteNormal);
        y = TablaSorteos(documento, ref canvas, estado, y, fuenteNormal, fuenteNegrita, fuenteBlancaNegrita);
        y = DibujarTotales(documento, ref canvas, estado, y, fuenteNegrita);
        DibujarPie(canvas, fuenteBlancaNegrita, fuenteBlanca);

        documento.EndPage();
        documento.Close();

        return Task.FromResult(ruta);
    }

    /// <summary>Logo + título + datos del vendedor (izq.) + tabla resumen (der.).</summary>
    private static float DibujarEncabezado(
        SKCanvas canvas, EstadoCuenta estado, float y,
        SKPaint fuenteTitulo, SKPaint fuenteNegrita, SKPaint fuenteNormal)
    {
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

        return Math.Max(yIzq, yRes) + 12f;
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
    /// Total) y las filas de sorteos con PAGINACIÓN por fila (la que
    /// no cabe pasa a página nueva con encabezado repetido).
    /// FASE 4: filas reales del endpoint (solo consignaciones).
    /// </summary>
    private static float TablaSorteos(
        SKDocument documento,
        ref SKCanvas canvas,
        EstadoCuenta estado,
        float y,
        SKPaint fuenteNormal,
        SKPaint fuenteNegrita,
        SKPaint fuenteBlancaNegrita)
    {
        // Columnas: Sorteo | Fecha | Cantidad | Vencido | Consigna | Pagos | Total
        float[] xs = { 36f, 250f, 320f, 380f, 440f, 500f, Ancho - 36f };

        bool sinEncabezado = true;
        foreach (SorteoEstadoCuenta s in estado.Sorteos)
        {
            if (sinEncabezado || y + Linea > Alto - 140f)
            {
                if (!sinEncabezado)
                {
                    NuevaPagina(documento, ref canvas, ref y);
                }
                DibujarEncabezadoTabla(canvas, xs, y, fuenteBlancaNegrita);
                y += 16f + 8f;
                sinEncabezado = false;
            }

            DibujarTexto(canvas, fuenteNegrita, s.Sorteo, xs[0], y);
            DibujarTexto(canvas, fuenteNormal, s.Fecha.ToString("dd-MM-yyyy", CultureInfo.CurrentCulture), xs[1], y);
            DibujarDerecha(canvas, fuenteNormal, s.Cantidad.ToString(CultureInfo.CurrentCulture), xs[2] + 30f, y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(s.Vencido), xs[3] + 40f, y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(s.Consigna), xs[4] + 40f, y);
            DibujarDerecha(canvas, fuenteNormal, Moneda(s.Pagos), xs[5] + 40f, y);
            DibujarDerecha(canvas, fuenteNegrita, Moneda(s.Total), xs[6], y);
            y += Linea;
        }

        // Sin sorteos: solo el encabezado (respaldo, p. ej. billetero
        // sin consignaciones vivas).
        if (sinEncabezado)
        {
            DibujarEncabezadoTabla(canvas, xs, y, fuenteBlancaNegrita);
            y += 16f + 8f;
        }

        return y;
    }

    /// <summary>Barra de encabezado roja con los 7 títulos (repetida por página).</summary>
    private static void DibujarEncabezadoTabla(SKCanvas canvas, float[] xs, float y, SKPaint fuenteBlancaNegrita)
    {
        using var barra = new SKPaint { Color = RojoTabla, Style = SKPaintStyle.Fill };
        using var borde = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), barra);
        canvas.DrawRect(SKRect.Create(34f, y, Ancho - 68f, 16f), borde);

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
    }

    /// <summary>Totales generales al pie de la tabla (Vencido/Consigna/Pagos/Total).</summary>
    private static float DibujarTotales(
        SKDocument documento, ref SKCanvas canvas, EstadoCuenta estado, float y, SKPaint fuenteNegrita)
    {
        y += 4f;
        if (y + Linea * 5f > Alto - 74f)
        {
            NuevaPagina(documento, ref canvas, ref y);
        }
        DibujarDerecha(canvas, fuenteNegrita, $"Vencido: {Moneda(estado.VencidoTotal)}", Ancho - 36f, y);
        y += Linea;
        DibujarDerecha(canvas, fuenteNegrita, $"Consigna: {Moneda(estado.ConsignaTotal)}", Ancho - 36f, y);
        y += Linea;
        DibujarDerecha(canvas, fuenteNegrita, $"Pagos: {Moneda(estado.PagosTotal)}", Ancho - 36f, y);
        y += Linea;
        DibujarDerecha(canvas, fuenteNegrita, $"Total: {Moneda(estado.TotalFinal)}", Ancho - 36f, y);
        return y + Linea;
    }

    /// <summary>Pie con fondo rojo institucional (solo en la página final).</summary>
    private static void DibujarPie(SKCanvas canvas, SKPaint fuenteBlancaNegrita, SKPaint fuenteBlanca)
    {
        using var fondoPie = new SKPaint { Color = RojoPie, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(0f, Alto - 74f, Ancho, 74f), fondoPie);

        float yPie = Alto - 48f;
        DibujarCentrado(canvas, fuenteBlancaNegrita, "Comercializadora de la Suerte S.A de C.V", yPie);
        yPie += Linea;
        DibujarCentrado(canvas, fuenteBlanca, "Calle Segunda Ote. Sur, Col. Centro, C.P. 29000, Tuxtla Gutiérrez, Chis.", yPie);
        yPie += Linea;
        DibujarCentrado(canvas, fuenteBlanca, "Teléfono: (961) 6120235", yPie);
    }

    /// <summary>
    /// FASE 4: cierra la página actual y abre una nueva con margen
    /// superior estándar; el pie institucional se dibuja solo en la
    /// página final.
    /// </summary>
    private static void NuevaPagina(SKDocument documento, ref SKCanvas canvas, ref float y)
    {
        documento.EndPage();
        canvas = documento.BeginPage(Ancho, Alto);
        y = 30f;
    }

    /// <summary>Factoría de fuentes (evita repetir el inicializador).</summary>
    private static SKPaint Fuente(SKColor color, float tamano, bool negrita) => new()
    {
        Color = color,
        TextSize = tamano,
        IsAntialias = true,
        Typeface = negrita ? SKTypeface.FromFamilyName(null, SKTypefaceStyle.Bold) : null,
    };

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
