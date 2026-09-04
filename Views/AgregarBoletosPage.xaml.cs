using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class AgregarBoletosPage : ContentPage
{
    public AgregarBoletosPage(AgregarBoletosViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
