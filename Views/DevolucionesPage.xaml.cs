using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Devoluciones" (mockup 6): historial persistente del
/// billetero (GET api/mobile/devoluciones) + FAB "+".
/// </summary>
public partial class DevolucionesPage : ContentPage
{
    private readonly DevolucionesViewModel _vm;

    public DevolucionesPage(DevolucionesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.AlAparecerAsync();
    }
}
