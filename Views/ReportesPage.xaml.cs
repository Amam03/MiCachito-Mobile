using MiCachito.Mobile.Controls;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>Reportes (mockups 9.x): pestañas Estado de Cuenta / Fondo de Ahorro / Facturación.</summary>
public partial class ReportesPage : ContentPage
{
    private readonly ReportesViewModel _vm;
    private readonly PieChartDrawable _pieDrawable = new();

    public ReportesPage(ReportesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;

        PieFacturacion.Drawable = _pieDrawable;
        _vm.PieSolicitaRedibujo += (s, e) => ActualizarPie();
    }

    /// <summary>
    /// Absorbe el tap sobre la card del modal "Seleccionar Fechas" para
    /// que no se propague al overlay de fondo (que sí cierra).
    /// </summary>
    private void CardModal_Tapped(object? sender, TappedEventArgs e)
    {
        // Intencionalmente vacío: solo consume el gesto.
    }

    /// <summary>
    /// Refresca el pie con los segmentos actuales y reconstruye la
    /// leyenda (indicador de color + nombre + monto + porcentaje).
    /// </summary>
    private void ActualizarPie()
    {
        _pieDrawable.Segmentos = _vm.FacSegmentos;
        PieFacturacion.Invalidate();

        LeyendaFacturacion.Clear();
        foreach (CategoriaFacturacion c in _vm.FacCategorias)
        {
            var indicador = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
                {
                    CornerRadius = 6
                },
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb(c.ColorHex),
                StrokeThickness = 0,
                WidthRequest = 14,
                HeightRequest = 14,
                VerticalOptions = LayoutOptions.Center,
            };

            var nombre = new Label
            {
                Text = c.Nombre,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1A1A1A"),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(6, 0, 10, 0),
            };

            var monto = new Label
            {
                Text = FormatoMoneda(c.Monto),
                FontSize = 13,
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1A1A1A"),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 10, 0),
            };

            var porcentaje = new Label
            {
                Text = FormatoPorcentaje(c.Porcentaje),
                FontSize = 13,
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#757575"),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 16, 0),
            };

            var fila = new HorizontalStackLayout { Children = { indicador, nombre, monto, porcentaje } };
            LeyendaFacturacion.Add(fila);
        }
    }

    /// <summary>Moneda: "$ 1,235.50".</summary>
    private static string FormatoMoneda(decimal v) =>
        $"$ {v.ToString("N2", System.Globalization.CultureInfo.CurrentCulture)}";

    /// <summary>Porcentaje con 1 decimal: "61.8%".</summary>
    private static string FormatoPorcentaje(double p) =>
        $"{p.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture)}%";
}
