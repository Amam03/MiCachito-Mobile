using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Depósitos" (mockups 7/7.3): listado en memoria + FAB "+".
/// Fase solo-interfaz.
/// </summary>
public partial class DepositosPage : ContentPage
{
    private readonly DepositosViewModel _vm;

    public DepositosPage(DepositosViewModel vm)
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
