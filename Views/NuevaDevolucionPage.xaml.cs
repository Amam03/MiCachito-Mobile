using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Pantalla "Nueva Devolución" (mockup 6.2): sorteo en verde, modos de
/// captura, Escanear, contadores con flecha y Guardar/Cancelar (mantener
/// presionado). El press-and-hold de Cancelar se maneja aquí con un
/// temporizador simple (Pressed/Released del botón).
/// Fase solo-interfaz.
/// </summary>
public partial class NuevaDevolucionPage : ContentPage
{
    private readonly NuevaDevolucionViewModel _vm;
    private System.Timers.Timer? _holdTimer;

    public NuevaDevolucionPage(NuevaDevolucionViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        BotonCancelar.Pressed += OnCancelarPressed;
        BotonCancelar.Released += OnCancelarReleased;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.AlAparecer();
    }

    /// <summary>Mantener presionado ~2.5 s: ejecuta la cancelación real.</summary>
    private void OnCancelarPressed(object? sender, EventArgs e)
    {
        _holdTimer?.Stop();
        _holdTimer?.Dispose();
        _holdTimer = new System.Timers.Timer(2500) { AutoReset = false };
        _holdTimer.Elapsed += (_, _) =>
        {
            _holdTimer.Stop();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _vm.AvisoCancelarVisible = false;
                if (_vm.CancelarMantenidoCommand.CanExecute(null))
                {
                    _vm.CancelarMantenidoCommand.Execute(null);
                }
            });
        };
        _holdTimer.Start();
    }

    /// <summary>Soltar antes de tiempo: cancela el temporizador; el tap ya
    /// mostró el aviso vía command.</summary>
    private void OnCancelarReleased(object? sender, EventArgs e)
    {
        _holdTimer?.Stop();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _holdTimer?.Stop();
    }
}
