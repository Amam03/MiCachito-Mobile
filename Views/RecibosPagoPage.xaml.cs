using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Recibos de Pago" (mockup 8): lista cronológica inversa con
/// Folio/Fecha/Total y check verde. Fase solo-interfaz.
/// </summary>
public partial class RecibosPagoPage : ContentPage
{
    public RecibosPagoPage(RecibosPagoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as RecibosPagoViewModel)?.AlAparecer();
    }
}
