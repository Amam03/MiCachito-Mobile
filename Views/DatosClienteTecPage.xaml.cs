using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class DatosClienteTecPage : ContentPage
{
    public DatosClienteTecPage(DatosClienteTecViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
