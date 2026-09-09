using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM de la lista "Recibos de Pago" (mockup 8): cards con Folio, Fecha,
/// Total y check verde de confirmación, en orden cronológico inverso.
/// Fase solo-interfaz.
/// </summary>
public partial class RecibosPagoViewModel : BaseViewModel
{
    private readonly RecibosPagoService _recibos;

    /// <summary>Recibos en orden cronológico inverso.</summary>
    public ObservableCollection<ReciboPago> Recibos { get; } = new();

    public RecibosPagoViewModel(RecibosPagoService recibos)
    {
        _recibos = recibos;
        Title = "Recibos de Pago";
        AlAparecer();
    }

    /// <summary>Carga la lista al entrar (y al volver del detalle).</summary>
    public void AlAparecer()
    {
        Recibos.Clear();
        foreach (ReciboPago r in _recibos.Recibos())
        {
            Recibos.Add(r);
        }
    }

    /// <summary>Abre el detalle del recibo seleccionado.</summary>
    [RelayCommand]
    private async Task SeleccionarReciboAsync(ReciboPago recibo)
    {
        if (recibo is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(
            $"{nameof(Views.DetallePagoPage)}?folio={recibo.Folio}",
            animate: true);
    }

    /// <summary>Regresa al menú de Gestión.</summary>
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
