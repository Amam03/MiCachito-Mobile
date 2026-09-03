using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class SorteosActivosPage : ContentPage
{
    public SorteosActivosPage(SorteosActivosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
