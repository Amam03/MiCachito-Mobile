using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class VentaExitosaLotenalPage : ContentPage
{
    public VentaExitosaLotenalPage(VentaExitosaLotenalViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
