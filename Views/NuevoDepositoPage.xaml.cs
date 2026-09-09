using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Nuevo Depósito" (mockups 7.1/7.2): formulario con selectores,
/// dial-pad del monto y validaciones rojas. Fase solo-interfaz.
/// </summary>
public partial class NuevoDepositoPage : ContentPage
{
    public NuevoDepositoPage(NuevoDepositoViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
