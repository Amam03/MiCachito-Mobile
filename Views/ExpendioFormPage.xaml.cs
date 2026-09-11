using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Formulario de Expendio (mockups 5/6): MISMA estructura para Actualizar y
/// Crear (título y botón dinámicos). Carga vía [QueryProperty] en AlAparecer.
/// </summary>
public partial class ExpendioFormPage : ContentPage
{
    private readonly ExpendioFormViewModel _vm;

    public ExpendioFormPage(ExpendioFormViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.AlAparecer();
    }
}
