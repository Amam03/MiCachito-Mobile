using Microsoft.Maui.Graphics;

namespace MiCachito.Mobile.Controls;

/// <summary>
/// Dibuja la gráfica de rendimiento del Fondo de Ahorro (mockup 9.2) con
/// el ICanvas nativo de MAUI (GraphicsView, patrón PieChartDrawable):
/// línea azul #3A4DBC con área lavanda #CACEE7 sobre la cuadrícula tenue
/// #E3E3E3. Serie = SALDO ACUMULADO por fecha del rango (interpretación
/// declarada y aprobada en el plan de Reportes): arranca en saldo_inicial
/// y suma cada movimiento (aportación +, retiro −).
///
/// Estado SIN datos (saldo plano o serie de 1 punto): cuadrícula + línea
/// base, sin puntos inventados.
/// </summary>
public class LineaSaldoDrawable : IDrawable
{
    /// <summary>Puntos a dibujar (x = 0..1 del rango, y = saldo acumulado).</summary>
    public IReadOnlyList<(double Fraccion, decimal Saldo)> Puntos { get; set; } =
        Array.Empty<(double, decimal)>();

    /// <summary>Saldo inicial del periodo (primer punto de la serie).</summary>
    public decimal SaldoInicial { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float izq = dirtyRect.X + 4f;
        float der = dirtyRect.X + dirtyRect.Width - 4f;
        float arriba = dirtyRect.Y + 6f;
        float abajo = dirtyRect.Y + dirtyRect.Height - 4f;

        // Cuadrícula tenue (misma que el estado sin datos del mockup 9.2)
        canvas.StrokeColor = Color.FromArgb("#E3E3E3");
        canvas.StrokeSize = 1f;
        for (int i = 0; i <= 4; i++)
        {
            float y = arriba + (abajo - arriba) * i / 4f;
            canvas.DrawLine(izq, y, der, y);
        }

        for (int i = 0; i <= 4; i++)
        {
            float x = izq + (der - izq) * i / 4f;
            canvas.DrawLine(x, arriba, x, abajo);
        }

        if (Puntos.Count == 0)
        {
            return; // estado sin datos: solo cuadrícula + línea base abajo
        }

        // Rango de la serie: si todos los saldos son iguales (plano, incl. 0)
        // la línea va al fondo sin inventar escala.
        decimal min = Puntos.Min(p => p.Saldo);
        decimal max = Puntos.Max(p => p.Saldo);
        if (max == min)
        {
            max = min + 1m; // evita división entre 0; línea plana visible
        }

        float Ancho(double f) => (float)(izq + (der - izq) * f);
        float Alto(decimal saldo) => (float)(abajo - (abajo - arriba) * (double)(saldo - min) / (double)(max - min));

        // Área lavanda bajo la curva (fill del polígono línea + base).
        PathF area = new PathF();
        area.MoveTo(Ancho(Puntos[0].Fraccion), Alto(Puntos[0].Saldo));
        foreach ((double fraccion, decimal saldo) in Puntos.Skip(1))
        {
            area.LineTo(Ancho(fraccion), Alto(saldo));
        }

        area.LineTo(Ancho(Puntos[^1].Fraccion), abajo);
        area.LineTo(Ancho(Puntos[0].Fraccion), abajo);
        area.Close();
        canvas.FillColor = Color.FromArgb("#CACEE7");
        canvas.FillPath(area);

        // Línea azul encima del área
        PathF linea = new PathF();
        linea.MoveTo(Ancho(Puntos[0].Fraccion), Alto(Puntos[0].Saldo));
        foreach ((double fraccion, decimal saldo) in Puntos.Skip(1))
        {
            linea.LineTo(Ancho(fraccion), Alto(saldo));
        }

        canvas.StrokeColor = Color.FromArgb("#3A4DBC");
        canvas.StrokeSize = 2f;
        canvas.DrawPath(linea);
    }
}
