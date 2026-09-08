using SkiaSharp;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Services;

/// <summary>
/// Genera el PDF "Ticket de Venta" (mockup 5.2) con SkiaSharp 1.57 (el
/// ya presente vía ZXing — sin paquetes nuevos). Replica la estructura
/// del PDF de referencia: página carta (612×792 pt), logo arriba,
/// título, Plaza/CEDIS, vendedor, Fecha|Folio, Datos del Cliente,
/// tabla de desglose, Total, info adicional Tec, leyendas y Fecha y
/// Hora del Movimiento. Fase SOLO INTERFAZ: los datos vienen del
/// movimiento en memoria (fuente real: backend, ver
/// docs/NOTAS_TICKETS_VENTA.md).
/// </summary>
public class TicketPdfService
{
    /// <summary>Alto de línea de texto normal (pt del PDF de referencia).</summary>
    private const float Linea = 14.5f;

    /// <summary>Ancho carta en puntos.</summary>
    private const float Ancho = 612f;

    /// <summary>
    /// Genera el PDF del movimiento y devuelve la ruta absoluta del
    /// archivo en AppDataDirectory (ticket-{tipo}-{folio}.pdf).
    /// </summary>
    public Task<string> GenerarAsync(MovimientoVenta movimiento)
    {
        string dir = FileSystem.AppDataDirectory;
        string ruta = Path.Combine(dir, movimiento.NombreArchivoPdf + ".pdf");

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
            TextSize = 16f,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(null, SKTypefaceStyle.Bold),
        };

        float y = 24f;

        // ── Logo Mi Cachito (extraído del PDF de referencia) ──────────
        using SKBitmap? logo = CargarLogo();
        if (logo is not null)
        {
            // En el PDF de referencia: ~117×96 pt centrado arriba
            float escala = 96f / logo.Height;
            float w = logo.Width * escala;
            canvas.DrawBitmap(logo, (Ancho - w) / 2f, y, new SKPaint { FilterQuality = SKFilterQuality.Medium });
            y += 96f + 8f;
        }

        // ── Título "Ticket de Venta" ──────────────────────────────────
        DibujarCentrado(canvas, fuenteTitulo, "Ticket de Venta", y);
        y += 22f;

        // ── Plaza | CEDIS ─────────────────────────────────────────────
        DibujarCentrado(canvas, fuenteNormal, $"{movimiento.Plaza}    {movimiento.Cedis}", y);
        y += Linea;

        // ── Vendedor ──────────────────────────────────────────────────
        DibujarCentrado(canvas, fuenteNormal, movimiento.Vendedor, y);
        y += Linea;

        // ── Fecha: dd/MM/yyyy | Folio: N ──────────────────────────────
        string fecha = movimiento.Fecha.ToString("dd/MM/yyyy");
        DibujarCentrado(
            canvas,
            fuenteNormal,
            $"Fecha: {fecha}    Folio: {movimiento.Folio}",
            y);
        y += Linea + 8f;

        // ── Datos del Cliente ─────────────────────────────────────────
        DibujarCentrado(canvas, fuenteNormal, "Datos del Cliente", y);
        y += Linea;

        string nombre = movimiento.ClienteNombrePdf ?? "PÚBLICO EN GENERAL";
        string telefono = movimiento.ClienteTelefonoPdf ?? movimiento.Telefono ?? string.Empty;
        string nombreTelefono = string.IsNullOrEmpty(telefono)
            ? $"Nombre: {nombre}"
            : $"Nombre: {nombre}, Teléfono: {telefono}";
        DibujarCentrado(canvas, fuenteNormal, nombreTelefono, y);
        y += Linea;

        if (!string.IsNullOrEmpty(movimiento.ClienteCorreoPdf))
        {
            DibujarCentrado(
                canvas,
                fuenteNormal,
                $"Correo: {movimiento.ClienteCorreoPdf}",
                y);
            y += Linea;
        }

        y += 6f;

        // ── Tabla de desglose ─────────────────────────────────────────
        // Columnas del PDF de referencia (x en pt): Sorteo 34.5,
        // Boleto 361.2, Precio 544.8 (right-aligned ~578)
        bool esTecOLotenal = movimiento.Tipo is TipoVenta.SorteosTec or TipoVenta.Lotenal;

        if (esTecOLotenal)
        {
            DibujarTexto(canvas, fuenteNegrita, "Sorteo", 34.5f, y);
            DibujarTexto(canvas, fuenteNegrita, "Boleto", 361.2f, y);
            DibujarDerecha(canvas, fuenteNegrita, "Precio", 578.5f, y);
            y += Linea + 4f;

            DibujarTexto(canvas, fuenteNormal, movimiento.Sorteo ?? string.Empty, 33.7f, y);
            DibujarTexto(canvas, fuenteNormal, movimiento.Boleto ?? string.Empty, 360.4f, y);
            DibujarDerecha(canvas, fuenteNormal, $"${movimiento.Valor ?? movimiento.Importe:0.00}", 578.2f, y);
            y += Linea + 6f;
        }
        else
        {
            // Tiempo Aire: Compañía | Teléfono | Precio (misma tabla)
            DibujarTexto(canvas, fuenteNegrita, "Compañía", 34.5f, y);
            DibujarTexto(canvas, fuenteNegrita, "Teléfono", 361.2f, y);
            DibujarDerecha(canvas, fuenteNegrita, "Precio", 578.5f, y);
            y += Linea + 4f;

            DibujarTexto(canvas, fuenteNormal, movimiento.Compania ?? string.Empty, 33.7f, y);
            DibujarTexto(canvas, fuenteNormal, movimiento.Telefono ?? string.Empty, 360.4f, y);
            DibujarDerecha(canvas, fuenteNormal, $"${movimiento.Importe:0.00}", 578.2f, y);
            y += Linea + 6f;
        }

        // ── Total ─────────────────────────────────────────────────────
        DibujarDerecha(canvas, fuenteNegrita, $"${movimiento.Importe:0.00}", 578.2f, y);
        y += Linea + 10f;

        // ── Info adicional (Sorteos Tec) ──────────────────────────────
        if (movimiento.Tipo == TipoVenta.SorteosTec
            && !string.IsNullOrEmpty(movimiento.FolioCompraTec))
        {
            DibujarTexto(
                canvas,
                fuenteNormal,
                $"Folio de Compra de Sorteos Tec: {movimiento.FolioCompraTec}",
                36f,
                y);
            y += Linea + 2f;

            // Leyenda de certificados (2 líneas, ancho completo)
            y = DibujarParrafo(
                canvas,
                fuenteNormal,
                "Los certificados digitales de los boletos comprados serán "
                + "enviados al correo electrónico que aparece en la sección "
                + "DATOS DEL CLIENTE tan pronto como Sorteos Tec los emita.",
                34f,
                578f,
                y);
            y += 8f;
        }

        // ── GRACIAS POR SU COMPRA ─────────────────────────────────────
        DibujarCentrado(canvas, fuenteNegrita, "GRACIAS POR SU COMPRA", y);
        y += Linea + 4f;

        // ── Fecha y Hora del Movimiento ───────────────────────────────
        DibujarCentrado(
            canvas,
            fuenteNormal,
            $"Fecha y Hora del Movimiento: {movimiento.FechaHoraMovimiento:yyyy-MM-dd HH:mm:ss.fff}",
            y);
        y += Linea + 4f;

        // ── Soporte WhatsApp ──────────────────────────────────────────
        DibujarCentrado(
            canvas,
            fuenteNormal,
            "Para cualquier comentario, queja o aclaración favor de comunicarse al whatsapp 961 103 5921",
            y);

        documento.EndPage();
        documento.Close();

        return Task.FromResult(ruta);
    }

    // ── Helpers de dibujo ─────────────────────────────────────────────

    /// <summary>Carga el logo del paquete; null si no existe.</summary>
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
    {
        canvas.DrawText(texto, x, y, fuente);
    }

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

    /// <summary>Dibuja un párrafo con corte de palabra; devuelve la y final.</summary>
    private static float DibujarParrafo(
        SKCanvas canvas,
        SKPaint fuente,
        string texto,
        float x,
        float xFin,
        float y)
    {
        float anchoMax = xFin - x;
        float yActual = y;
        string[] palabras = texto.Split(' ');
        string linea = string.Empty;

        foreach (string palabra in palabras)
        {
            string prueba = string.IsNullOrEmpty(linea) ? palabra : linea + " " + palabra;
            if (fuente.MeasureText(prueba) > anchoMax && !string.IsNullOrEmpty(linea))
            {
                DibujarTexto(canvas, fuente, linea, x, yActual);
                yActual += Linea;
                linea = palabra;
            }
            else
            {
                linea = prueba;
            }
        }

        if (!string.IsNullOrEmpty(linea))
        {
            DibujarTexto(canvas, fuente, linea, x, yActual);
            yActual += Linea;
        }

        return yActual;
    }
}
