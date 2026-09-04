using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class SorteosLotenalPage : ContentPage
{
    public SorteosLotenalPage(SorteosLotenalViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
