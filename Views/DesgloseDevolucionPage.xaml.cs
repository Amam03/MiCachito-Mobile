using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla de desglose de captura (mockup 6.2): tabla Billete | Cantidad |
/// Vigésimo | Serie del modo pedido. Fase solo-interfaz.
/// </summary>
public partial class DesgloseDevolucionPage : ContentPage
{
    private readonly DesgloseDevolucionViewModel _vm;

    public DesgloseDevolucionPage(DesgloseDevolucionViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.AlAparecer();
    }
}
