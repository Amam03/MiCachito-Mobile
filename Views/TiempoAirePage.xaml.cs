using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class TiempoAirePage : ContentPage
{
    public TiempoAirePage(TiempoAireViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}