using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>Notificaciones (mockup 3): estado vacío en esta fase.</summary>
public partial class NotificacionesPage : ContentPage
{
    public NotificacionesPage(NotificacionesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
