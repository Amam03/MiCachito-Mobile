using Microsoft.Maui.Graphics;

namespace MiCachito.Mobile.Controls;

/// <summary>
/// Dibuja la gráfica circular de Facturación (mockup 9.3) con el
/// ICanvas nativo de MAUI (GraphicsView): segmentos proporcionales al
/// monto de cada categoría y el porcentaje DENTRO de cada segmento
/// (cálculo dinámico, nunca fijo). Sin paquetes externos.
/// </summary>
public class PieChartDrawable : IDrawable
{
    /// <summary>Segmentos a dibujar (nombre, color, porcentaje 0-100).</summary>
    public IReadOnlyList<(string Nombre, string ColorHex, double Porcentaje)> Segmentos { get; set; } =
        Array.Empty<(string, string, double)>();

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

            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 2f;
            canvas.FillColor = Color.FromArgb(colorHex);
            float fin = inicio + barrido;
            canvas.FillArc(cx - radio, cy - radio, radio * 2f, radio * 2f, inicio, fin, true);
            canvas.DrawArc(cx - radio, cy - radio, radio * 2f, radio * 2f, inicio, fin, true, false);
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

    /// <summary>Porcentaje con 1 decimal ("48.7%").</summary>
    private static string FormatoPorcentaje(double p) =>
        $"{p.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture)}%";
}
