using Microsoft.Maui.Graphics;

namespace MiCachito.Mobile.Controls;

/// <summary>
/// Dibuja la gráfica circular de Facturación (mockup 9.3) con el
/// ICanvas nativo de MAUI (GraphicsView): segmentos proporcionales al
/// monto de cada categoría y el porcentaje DENTRO de cada segmento
/// (cálculo dinámico, nunca fijo). Sin paquetes externos.
///
/// FASE 3 (fix 2026-09-22): FillArc/DrawArc de ICanvas renderizan MAL
/// estos arcos en Android (verificado por análisis de píxeles: con
/// segmentos Mayor 6.1% / Zodiaco 3.0% / Mi Sueño 90.9% el resultado
/// era un círculo COMPLETO rosa — el slice de 327° no pintaba nada y
/// uno pequeño cubría todo). Las rebanadas se dibujan ahora como PATHS
/// POLIGONALES: el arco se muestrea punto por punto (paso 1.5°) y se
/// rellena con FillPath — la geometría queda bajo nuestro control y es
/// inmune a la semántica del adapter de plataforma.
/// </summary>
public class PieChartDrawable : IDrawable
{
    /// <summary>Segmentos a dibujar (nombre, color, porcentaje 0-100).</summary>
    public IReadOnlyList<(string Nombre, string ColorHex, double Porcentaje)> Segmentos { get; set; } =
        Array.Empty<(string, string, double)>();

    /// <summary>Paso de muestreo del arco en grados (1.5° ≈ error de cuerda &lt; 0.1 px a r=280).</summary>
    private const float Paso = 1.5f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Segmentos.Count == 0)
        {
            return;
        }

        float tamano = Math.Min(dirtyRect.Width, dirtyRect.Height);
        float cx = dirtyRect.Center.X;
        float cy = dirtyRect.Center.Y;
        float radio = tamano / 2f - 6f;

        float inicio = -90f; // arranca arriba (12 h), como la referencia
        foreach ((string nombre, string colorHex, double porcentaje) in Segmentos)
        {
            float barrido = (float)(porcentaje / 100.0 * 360.0);
            if (barrido <= 0f)
            {
                continue;
            }

            PathF rebanada = ConstruirRebanada(cx, cy, radio, inicio, inicio + barrido);
            canvas.FillColor = Color.FromArgb(colorHex);
            canvas.FillPath(rebanada);
            // Borde blanco entre rebanadas (separador del mockup).
            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 2f;
            canvas.DrawPath(rebanada);
            inicio += barrido;
        }

        // Porcentaje dentro de cada segmento (solo si el segmento es legible)
        inicio = -90f;
        foreach ((string nombre, string colorHex, double porcentaje) in Segmentos)
        {
            float barrido = (float)(porcentaje / 100.0 * 360.0);
            double medio = inicio + barrido / 2f;
            if (barrido / 360f * 100 >= 8 && porcentaje > 0)
            {
                float px = cx + (float)Math.Cos(medio * Math.PI / 180.0) * radio * 0.62f;
                float py = cy + (float)Math.Sin(medio * Math.PI / 180.0) * radio * 0.62f;
                canvas.FontColor = Colors.White;
                canvas.FontSize = 14;
                canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
                string texto = FormatoPorcentaje(porcentaje);
                canvas.DrawString(
                    texto,
                    px - 22f, py - 8f, 44f, 20f,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center);
            }
            inicio += barrido;
        }
    }

    /// <summary>
    /// Construye la rebanada (centro → arco muestreado → cierre) como
    /// path poligonal. Convención de ángulos: 0° = derecha, positivo en
    /// sentido horario (y crece hacia abajo), -90° = 12 h.
    /// </summary>
    private static PathF ConstruirRebanada(float cx, float cy, float radio, float desde, float hasta)
    {
        var path = new PathF();
        path.MoveTo(cx, cy);
        for (float a = desde; a < hasta; a += Paso)
        {
            float rad = a * MathF.PI / 180f;
            path.LineTo(cx + MathF.Cos(rad) * radio, cy + MathF.Sin(rad) * radio);
        }
        // Punto final exacto del arco (evita el escalón del último paso).
        float radFin = hasta * MathF.PI / 180f;
        path.LineTo(cx + MathF.Cos(radFin) * radio, cy + MathF.Sin(radFin) * radio);
        path.Close();
        return path;
    }

    /// <summary>Porcentaje con 1 decimal ("48.7%").</summary>
    private static string FormatoPorcentaje(double p) =>
        $"{p.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture)}%";
}
