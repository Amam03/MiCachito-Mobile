using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class NumeroTelefonoTiempoAirePage : ContentPage
{
    public NumeroTelefonoTiempoAirePage(NumeroTelefonoTiempoAireViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
