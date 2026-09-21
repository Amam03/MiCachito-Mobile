using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Recibos de Pago" (mockup 8): lista cronológica inversa con
/// Folio/Fecha/Total y check verde. Conectada a
/// api/mobile/pagos/recibos. Sin bloque de crédito: la referencia no lo
/// muestra (verificado 2026-09-21).
/// </summary>
public partial class RecibosPagoPage : ContentPage
{
    private readonly RecibosPagoViewModel _viewModel;

    public RecibosPagoPage(RecibosPagoViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.AlAparecerAsync();
        }
        catch
        {
            // El VM ya maneja sus propios estados de error; esto solo evita
            // que una excepción no controlada rompa la navegación.
        }
    }
}
