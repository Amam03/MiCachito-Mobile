using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();

        BindingContext = new LoginViewModel();
    }
}