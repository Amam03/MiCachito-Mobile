using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Detalle de Pago" (mockups 8.1/8.2): header azul con Folio,
/// Fecha y Total, desglose de la operación, FAB de descarga con overlay
/// y aviso verde. Fase solo-interfaz.
/// </summary>
public partial class DetallePagoPage : ContentPage
{
    public DetallePagoPage(DetallePagoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as DetallePagoViewModel)?.AlAparecer();
    }
}
