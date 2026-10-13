using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Depósitos" (mockups 7/7.3): historial real del billetero
/// (GET api/mobile/depositos) + FAB "+". La nube ámbar de la tarjeta
/// corresponde a estatus 'pendiente' (sin validar por Caja).
/// </summary>
public partial class DepositosPage : ContentPage
{
    private readonly DepositosViewModel _vm;

    public DepositosPage(DepositosViewModel vm)
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
