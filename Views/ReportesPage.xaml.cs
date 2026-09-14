using MiCachito.Mobile.Controls;
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
    /// Refresca el pie con los segmentos actuales (la leyenda es
    /// BindableLayout sobre FacCategorias en el XAML y se actualiza sola).
    /// </summary>
    private void ActualizarPie()
    {
        _pieDrawable.Segmentos = _vm.FacSegmentos;
        PieFacturacion.Invalidate();
    }
}
