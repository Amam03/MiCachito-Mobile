using MiCachito.Mobile.Views;

namespace MiCachito.Mobile;

public partial class AppShell : Shell
{
    public AppShell(HomePage homePage)
    {
        InitializeComponent();

        Items.Add(new ShellContent
        {
            Title = "Inicio",
            Route = "HomePage",
            Content = homePage,
        });
    }
}
