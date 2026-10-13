using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Nuevo Depósito" (mockups 7.1/7.2): formulario con selectores,
/// dial-pad del monto y validaciones rojas. El selector de banco lista
/// las cuentas activas del backend (GET api/mobile/depositos/cuentas) y
/// Aceptar registra con POST api/mobile/depositos.
/// </summary>
public partial class NuevoDepositoPage : ContentPage
{
    private readonly NuevoDepositoViewModel _vm;

    public NuevoDepositoPage(NuevoDepositoViewModel vm)
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
