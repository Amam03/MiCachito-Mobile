using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Lista de Sorteos" de Devolución (mockup 6.1): sorteos activos
/// del catálogo LOTENAL. Fase solo-interfaz.
/// </summary>
public partial class ListaSorteosDevolucionPage : ContentPage
{
    private readonly ListaSorteosDevolucionViewModel _vm;

    public ListaSorteosDevolucionPage(ListaSorteosDevolucionViewModel vm)
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
