using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>Página de selección de impresora Bluetooth (F6).</summary>
public partial class ImpresoraPage : ContentPage
{
    public ImpresoraPage(ImpresoraViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
