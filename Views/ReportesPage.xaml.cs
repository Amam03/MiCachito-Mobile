using MiCachito.Mobile.Controls;
using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>Reportes (mockups 9.x): pestañas Estado de Cuenta / Fondo de Ahorro / Facturación.</summary>
public partial class ReportesPage : ContentPage
{
    private readonly ReportesViewModel _vm;
    private readonly PieChartDrawable _pieDrawable = new();
    private readonly LineaSaldoDrawable _faDrawable = new();

    public ReportesPage(ReportesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;

        PieFacturacion.Drawable = _pieDrawable;
        _vm.PieSolicitaRedibujo += (s, e) => ActualizarPie();

        // FASE 2: gráfica de Fondo de Ahorro con la serie real de saldo
        // acumulado del periodo (GraphicsView + drawable, patrón del pie).
        GraficaFondoAhorro.Drawable = _faDrawable;
        _vm.GraficaFaSolicitaRedibujo += (s, e) => ActualizarGraficaFa();
        ActualizarGraficaFa();
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
    /// Refresca el pie con los segmentos actuales (la leyenda es
    /// BindableLayout sobre FacCategorias en el XAML y se actualiza sola).
    /// </summary>
    private void ActualizarPie()
    {
        _pieDrawable.Segmentos = _vm.FacSegmentos;
        PieFacturacion.Invalidate();
    }

    /// <summary>
    /// FASE 2: refresca la gráfica de Fondo de Ahorro con la serie de
    /// saldo acumulado actual del periodo.
    /// </summary>
    private void ActualizarGraficaFa()
    {
        _faDrawable.Puntos = _vm.FaSerieSaldo;
        _faDrawable.SaldoInicial = _vm.FaSaldoInicial;
        GraficaFondoAhorro.Invalidate();
    }
}
