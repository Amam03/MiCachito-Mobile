using MiCachito.Mobile.Views;

namespace MiCachito.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new LoginPage();
    }
}