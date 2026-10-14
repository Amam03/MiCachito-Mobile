using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la captura (mockup 3.3): folio incremental, boton Escanear,
/// resumen y contadores EN VIVO, Guardar (verde) y Cancelar (mantener
/// presionado). El escaneo continuo vive en EscanearBoletosPage; aqui se
/// ve el acumulado del servicio compartido (singleton).
/// </summary>
public partial class CapturaPremiosReintegrosViewModel : BaseViewModel
{
    private readonly PremiosReintegrosService _servicio;

    public CapturaPremiosReintegrosViewModel(PremiosReintegrosService servicio)
    {
        _servicio = servicio;
        Title = "Captura Premios y Reintegros";
    }

    /// <summary>Boletos acumulados en la captura en curso (vivo).</summary>
    public ObservableCollection<BoletoCapturado> Captura => _servicio.CapturaActual;

    /// <summary>Refresca derivados al volver del escaner (patron del proyecto).</summary>
    public void AlAparecer()
    {
        OnPropertyChanged(nameof(FolioText));
        OnPropertyChanged(nameof(MontoPremiosText));
        OnPropertyChanged(nameof(MontoReintegrosText));
        OnPropertyChanged(nameof(MontoTotalText));
        OnPropertyChanged(nameof(ContadorPremiosText));
        OnPropertyChanged(nameof(ContadorReintegrosText));
        OnPropertyChanged(nameof(Captura));
        Aviso = string.Empty;
    }

    /// <summary>
    /// Folio en pantalla (mockup 3.3 "Folio: 3"): con la integración el
    /// folio PYR-YYYYMMDD-NNNN lo asigna el BACKEND al Guardar (D3), así
    /// que la captura muestra la cantidad de boletos pendientes de guardar.
    /// </summary>
    public string FolioText => $"Boletos: {_servicio.CapturaActual.Count:00}";

    public string MontoPremiosText => $"${_servicio.MontoPremios:0.00}";

    public string MontoReintegrosText => $"${_servicio.MontoReintegros:0.00}";

    public string MontoTotalText => $"${_servicio.MontoPremios + _servicio.MontoReintegros:0.00}";

    public string ContadorPremiosText => $"{_servicio.ContadorPremios:00}";

    public string ContadorReintegrosText => $"{_servicio.ContadorReintegros:00}";

    /// <summary>Aviso de Cancelar (tap simple) o errores; vacio = sin aviso.</summary>
    [ObservableProperty]
    private string aviso = string.Empty;

    /// <summary>Abre el escaner continuo (mockup 3.3 escaneo).</summary>
    [RelayCommand]
    private Task EscanearAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        Aviso = string.Empty;
        return Shell.Current.GoToAsync(nameof(Views.EscanearBoletosPage));
    }

    /// <summary>
    /// Guardar (D3): envía la captura al backend (POST), que asigna el
    /// folio PYR-YYYYMMDD-NNNN y los totales; limpia la captura SOLO si
    /// el guardado fue exitoso (sin conexión queda intacta para
    /// reintentar) y regresa a la lista.
    /// </summary>
    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (IsBusy || Shell.Current is null)
        {
            return;
        }

        if (_servicio.CapturaActual.Count == 0)
        {
            Aviso = "No hay boletos capturados en el movimiento.";
            return;
        }

        IsBusy = true;
        Aviso = "Guardando...";
        try
        {
            try
            {
                await _servicio.GuardarCapturaAsync();
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
            {
                Aviso = "Sin conexión al servidor. La captura quedó intacta: intenta de nuevo.";
                return;
            }
            catch (ApiException ex)
            {
                Aviso = $"No se pudo guardar el movimiento: {ex.Message}";
                return;
            }

            // Guardado OK: la captura ya se limpió en el servicio. Regresar
            // a la LISTA (un pop): ahi se ve el folio PYR- recien guardado
            // y el FAB sigue accesible. Navegacion RELATIVA —
            // "//GestionPage" CRASHEA: la pestana Gestion es un ShellContent
            // con DataTemplate (ruta implicita, no global registrada).
            await Shell.Current.GoToAsync("..");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Tap simple en Cancelar: solo avisa (no descarta nada).</summary>
    [RelayCommand]
    private void CancelarTap()
    {
        Aviso = "Manten presionado Cancelar para descartar la captura.";
    }

    /// <summary>Cancelar (mantener presionado): descarta y regresa a la lista.</summary>
    [RelayCommand]
    private Task CancelarLongPressAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        _servicio.CancelarCaptura();
        // Mismo fix que Guardar: pop a la lista, nunca "//GestionPage".
        return Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task GoBackAsync()
    {
        if (Shell.Current is not null)
        {
            return Shell.Current.GoToAsync("..");
        }
        return Task.CompletedTask;
    }
}
