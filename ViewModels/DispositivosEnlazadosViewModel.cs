using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// Dispositivos Enlazados (mockup 5). Muestra los dispositivos
/// Bluetooth VINCULADOS reales del teléfono (IImpresoraService — única
/// integración Bluetooth del proyecto). El emparejamiento/selección
/// real queda para la integración.
/// </summary>
public partial class DispositivosEnlazadosViewModel : BaseViewModel
{
    private readonly IImpresoraService _impresoraService;

    public ObservableCollection<string> Dispositivos { get; } = new();

    [ObservableProperty]
    private bool _sinDispositivos;

    public DispositivosEnlazadosViewModel(IImpresoraService impresoraService)
    {
        _impresoraService = impresoraService;
    }

    /// <summary>Consulta los dispositivos vinculados reales al aparecer.</summary>
    public async Task AlAparecerAsync()
    {
        Dispositivos.Clear();
        var dispositivos = await _impresoraService.ObtenerDispositivosEnlazadosAsync();
        foreach (var d in dispositivos)
        {
            Dispositivos.Add(d);
        }

        SinDispositivos = Dispositivos.Count == 0;
    }

    /// <summary>Regresa a Cuenta (flecha del header, mockup 5).</summary>
    [RelayCommand]
    private async Task VolverAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
