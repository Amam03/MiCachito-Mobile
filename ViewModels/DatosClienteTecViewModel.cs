using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla "Datos del Cliente" (pantalla 15), a la que
/// se llega desde Vender del carrito Tec (pantalla 14).
///
/// Formulario (mockup 15): Nombre, Apellido Paterno, Apellido Materno,
/// Teléfono, Correo Electrónico y Seleccionar Estado (picker con las 32
/// entidades federativas, catálogo EstadosData por decisión del usuario).
///
/// Solo UI + estado local (sin backend): Confirmar Venta limpia el
/// carrito (las plecas verdes del catálogo compartido) y navega a la
/// pantalla de Venta Exitosa.
/// </summary>
public partial class DatosClienteTecViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _nombre = string.Empty;

    [ObservableProperty]
    private string _apellidoPaterno = string.Empty;

    [ObservableProperty]
    private string _apellidoMaterno = string.Empty;

    [ObservableProperty]
    private string _telefono = string.Empty;

    [ObservableProperty]
    private string _correoElectronico = string.Empty;

    [ObservableProperty]
    private string? _estadoSeleccionado;

    public DatosClienteTecViewModel()
    {
        Title = "Datos del Cliente";
    }

    /// <summary>Estados del picker: 32 entidades federativas.</summary>
    public ObservableCollection<string> Estados { get; } = new(EstadosData.ObtenerEstados());

    /// <summary>
    /// Botón "Confirmar Venta": limpia el carrito (catálogo compartido)
    /// y abre la pantalla de Venta Exitosa. Sin backend por ahora.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmarVentaAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            foreach (var billete in SorteosTecData.BilletesAgregados().ToList())
            {
                billete.Agregado = false;
            }

            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(nameof(Views.VentaExitosaTecPage));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Retrocede a la pantalla anterior (carrito Tec).
    /// </summary>
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
