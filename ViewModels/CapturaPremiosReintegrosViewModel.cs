using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    /// <summary>Folio que tomara el movimiento al Guardar (ej. "Folio: 3").</summary>
    public string FolioText => $"Folio: {_servicio.FolioSiguiente}";

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
    /// Guardar (tap): materializa la captura en un movimiento nuevo con
    /// folio incremental, limpia la captura y regresa a la lista.
    /// </summary>
    [RelayCommand]
    private Task GuardarAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        if (_servicio.CapturaActual.Count == 0)
        {
            Aviso = "No hay boletos capturados en el movimiento.";
            return Task.CompletedTask;
        }

        _servicio.GuardarCaptura();
        // Regresar a la LISTA de movimientos (un pop): ahi se ve el folio
        // recien guardado y el FAB sigue accesible. Navegacion RELATIVA —
        // "//GestionPage" CRASHEA: la pestana Gestion es un ShellContent con
        // DataTemplate (ruta implicita, no global registrada).
        return Shell.Current.GoToAsync("..");
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
