using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>Reportes (mockups 9.x): pestañas Estado de Cuenta / Fondo de Ahorro / Facturación.</summary>
public partial class ReportesPage : ContentPage
{
    public ReportesPage(ReportesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    /// <summary>
    /// Absorbe el tap sobre la card del modal "Seleccionar Fechas" para
    /// que no se propague al overlay de fondo (que sí cierra).
    /// </summary>
    private void CardModal_Tapped(object? sender, TappedEventArgs e)
    {
        // Intencionalmente vacío: solo consume el gesto.
    }
}
