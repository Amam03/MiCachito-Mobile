using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class SeleccionCiudadPage : ContentPage
{
    public SeleccionCiudadPage(SeleccionCiudadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
