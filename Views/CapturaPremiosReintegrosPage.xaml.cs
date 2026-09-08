using MiCachito.Mobile.ViewModels;

namespace MiCachito.Mobile.Views;

/// <summary>
/// Code-behind de la captura (mockup 3.3). El Cancelar descarta la captura
/// SOLO con pulsacion larga (~800 ms, regla del usuario); un tap simple
/// solo muestra el aviso a traves del VM.
/// </summary>
public partial class CapturaPremiosReintegrosPage : ContentPage
{
    private readonly CapturaPremiosReintegrosViewModel _viewModel;
    private System.Threading.CancellationTokenSource? _ctsCancelar;

    public CapturaPremiosReintegrosPage(CapturaPremiosReintegrosViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Refresca folio/montos/contadores al volver del escaner (patron
        // del proyecto: derivados no se recalculan solos).
        _viewModel.AlAparecer();
    }

    /// <summary>Pulsacion sobre Cancelar inicia el temporizador de long-press.</summary>
    private void OnCancelarPressed(object? sender, EventArgs e)
    {
        _ctsCancelar?.Cancel();
        _ctsCancelar = new System.Threading.CancellationTokenSource();
        System.Threading.CancellationToken token = _ctsCancelar.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(800, token);
            }
            catch (TaskCanceledException)
            {
                return; // solto antes de tiempo: tap simple
            }

            if (!token.IsCancellationRequested)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        _viewModel.CancelarLongPressCommand.Execute(null);
                    }
                });
            }
        });
    }

    /// <summary>Soltar antes de 800 ms = tap simple: solo aviso, no descarta.</summary>
    private void OnCancelarReleased(object? sender, EventArgs e)
    {
        if (_ctsCancelar is { IsCancellationRequested: false } ctsActivo)
        {
            // Todavia no habia disparado el long-press: fue un tap corto.
            ctsActivo.Cancel();
            _viewModel.CancelarTapCommand.Execute(null);
        }
        _ctsCancelar?.Cancel();
    }
}
