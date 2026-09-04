using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class CarritoComprasPage : ContentPage
{
    public CarritoComprasPage(CarritoComprasViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
