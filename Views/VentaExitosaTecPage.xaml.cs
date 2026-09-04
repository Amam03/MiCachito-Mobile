using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class VentaExitosaTecPage : ContentPage
{
    public VentaExitosaTecPage(VentaExitosaTecViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
