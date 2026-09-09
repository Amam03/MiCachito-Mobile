using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Devoluciones" (mockup 6): listado en memoria + FAB "+".
/// Fase solo-interfaz.
/// </summary>
public partial class DevolucionesPage : ContentPage
{
    private readonly DevolucionesViewModel _vm;

    public DevolucionesPage(DevolucionesViewModel vm)
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
