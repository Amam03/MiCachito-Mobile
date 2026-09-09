using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel del desglose de captura (mockup 6.2 desglose): filas del modo
/// pedido (Cachitos/Tiras/Series) con tabla Billete | Cantidad | Vigésimo |
/// Serie. Solo filas realmente capturadas; inicia vacía. Recibe el modo
/// vía [QueryProperty] y carga en AlAparecer (patrón del proyecto).
/// Fase solo-interfaz.
/// </summary>
[QueryProperty(nameof(ModoQuery), "modo")]
public partial class DesgloseDevolucionViewModel : BaseViewModel
{
    private readonly DevolucionService _devoluciones;

    /// <summary>Modo cuyo desglose se muestra (recibido por navegación).</summary>
    [ObservableProperty]
    private string modoQuery = string.Empty;

    /// <summary>Filas del modo pedido.</summary>
    public ObservableCollection<FilaDesglose> Filas { get; } = new();

    /// <summary>Título "[Modo] Capturados (N)".</summary>
    public string TituloDesglose =>
        $"{NombreModo} Capturados ({Filas.Count})";

    /// <summary>Nombre del sorteo seleccionado (arriba de la tabla).</summary>
    public string NombreSorteo => _devoluciones.SorteoEnCurso?.NombreCorto ?? "-";

    /// <summary>Nombre legible del modo.</summary>
    public string NombreModo => Modo switch
    {
        ModoCapturaDevolucion.Cachitos => "Cachitos",
        ModoCapturaDevolucion.Tiras => "Tiras",
        _ => "Series",
    };

    /// <summary>Modo parseado (Series por defecto).</summary>
    public ModoCapturaDevolucion Modo =>
        Enum.TryParse<ModoCapturaDevolucion>(ModoQuery, out ModoCapturaDevolucion m)
            ? m
            : ModoCapturaDevolucion.Series;

    /// <summary>True si hay filas (inverso para el mensaje de vacío).</summary>
    public bool HayFilas => Filas.Count > 0;

    /// <summary>Inverso de HayFilas: muestra "Sin capturas en este modo".</summary>
    public bool HayFilasInverso => Filas.Count == 0;

    public DesgloseDevolucionViewModel(DevolucionService devoluciones)
    {
        _devoluciones = devoluciones;
        Title = "Desglose";
    }

    /// <summary>Carga las filas al aparecer.</summary>
    public void AlAparecer()
    {
        Filas.Clear();
        foreach (FilaDesglose f in _devoluciones.FilasEnCurso.Where(f => f.Modo == Modo))
        {
            Filas.Add(f);
        }

        OnPropertyChanged(nameof(TituloDesglose));
        OnPropertyChanged(nameof(NombreModo));
        OnPropertyChanged(nameof(NombreSorteo));
        OnPropertyChanged(nameof(HayFilas));
        OnPropertyChanged(nameof(HayFilasInverso));
    }

    /// <summary>Regresa a Nueva Devolución.</summary>
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
