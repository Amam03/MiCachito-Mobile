using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class SeleccionarBilletePage : ContentPage
{
    public SeleccionarBilletePage(SeleccionarBilleteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Al volver del carrito (pantalla 14) refresca el badge: Eliminar
    /// pudo quitar la pleca verde de billetes de este sorteo.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as SeleccionarBilleteViewModel)?.AlAparecer();
    }
}
