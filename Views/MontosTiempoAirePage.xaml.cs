using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class MontosTiempoAirePage : ContentPage
{
    public MontosTiempoAirePage(MontosTiempoAireViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
