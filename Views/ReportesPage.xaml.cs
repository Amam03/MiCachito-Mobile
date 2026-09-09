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
}
