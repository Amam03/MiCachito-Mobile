using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la lista de movimientos (mockup 3: resumen + listado +
/// FAB "+"). Navega al detalle (tap en registro) y a la captura (FAB).
/// Estado compartido via PremiosReintegrosService (singleton).
/// </summary>
public partial class PremiosReintegrosViewModel : BaseViewModel
{
    private readonly PremiosReintegrosService _servicio;

    public ObservableCollection<MovimientoPremiosReintegros> Movimientos => _servicio.Movimientos;

    public PremiosReintegrosViewModel(PremiosReintegrosService servicio)
    {
        _servicio = servicio;
        Title = "Premios y Reintegros";
    }

    /// <summary>
    /// Refresca los totales agregados al volver de captura/detalle (patron
    /// del proyecto: las propiedades derivadas NO se recalculan solas al
    /// volver de otra pagina).
    /// </summary>
    public void AlAparecer()
    {
        OnPropertyChanged(nameof(Movimientos));
        OnPropertyChanged(nameof(TotalPremiosText));
        OnPropertyChanged(nameof(TotalReintegrosText));
        OnPropertyChanged(nameof(TotalGeneralText));
    }

    public string TotalPremiosText => Formato(_servicio.TotalPremios);

    public string TotalReintegrosText => Formato(_servicio.TotalReintegros);

    public string TotalGeneralText => Formato(_servicio.TotalGeneral);

    private static string Formato(decimal v) => $"${v:0.00}";

    /// <summary>Tap en un registro: abre su detalle (folio por query param).</summary>
    [RelayCommand]
    private Task AbrirDetalleAsync(MovimientoPremiosReintegros movimiento)
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(
            $"{nameof(Views.DetallePremiosReintegrosPage)}?folio={movimiento.Folio}");
    }

    /// <summary>FAB "+": abre la pantalla de captura (mockup 3.3).</summary>
    [RelayCommand]
    private Task AgregarAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(nameof(Views.CapturaPremiosReintegrosPage));
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
