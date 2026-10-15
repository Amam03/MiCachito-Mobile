using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM del listado "Devoluciones" (mockup 6): historial PERSISTENTE del
/// billetero de la sesión (GET api/mobile/devoluciones) + FAB "+" hacia
/// la Lista de Sorteos (6.1). Estados: cargando (overlay), vacío, error
/// con re-entrada como reintento (patrón Depósitos/Recibos).
///
/// La carga reemplaza la lista con la del servidor (fuente de verdad);
/// las devoluciones guardadas en esta sesión reaparecen desde el
/// backend con el mismo id_movimiento (sin duplicados).
/// </summary>
public partial class DevolucionesViewModel : BaseViewModel
{
    private readonly DevolucionService _devoluciones;

    /// <summary>Devoluciones del billetero (más reciente primero).</summary>
    public ObservableCollection<Devolucion> Devoluciones { get; } = new();

    /// <summary>True mientras corre el GET del historial (overlay).</summary>
    [ObservableProperty]
    private bool cargando;

    /// <summary>Mensaje de error de la carga (reintentar re-entrando).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True cuando hay mensaje de error (muestra el aviso rojo).</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>True cuando la carga terminó sin devoluciones (estado vacío).</summary>
    public bool EstaVacio => !Cargando && !HayError && Devoluciones.Count == 0;

    /// <summary>True cuando no hay registros (mensaje del mockup 6).</summary>
    public bool SinRegistros => Devoluciones.Count == 0 && !Cargando && !HayError;

    public DevolucionesViewModel(DevolucionService devoluciones)
    {
        _devoluciones = devoluciones;
        Title = "Devoluciones";
    }

    /// <summary>
    /// Carga el historial persistente al entrar (y al volver de Nueva
    /// Devolución tras GUARDAR): servidor = fuente de verdad.
    /// </summary>
    public async Task AlAparecerAsync()
    {
        if (Cargando)
        {
            return;
        }

        Cargando = true;
        MensajeError = string.Empty;
        try
        {
            try
            {
                await _devoluciones.CargarHistorialAsync();
                Devoluciones.Clear();
                foreach (Devolucion d in _devoluciones.Devoluciones)
                {
                    Devoluciones.Add(d);
                }
            }
            catch (TaskCanceledException)
            {
                MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            }
            catch (OperationCanceledException)
            {
                MensajeError = "Consulta cancelada";
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                MensajeError = "Sin conexión al servidor. Verifica la conexión e intenta de nuevo";
            }
            catch (ApiException ex)
            {
                MensajeError = $"No se pudieron cargar las devoluciones: {ex.Message}";
            }
        }
        finally
        {
            Cargando = false;
            // Derivadas notificadas al FINAL (patrón Depósitos/Recibos):
            // notificarlas dentro del try dejaba el estado vacío oculto.
            OnPropertyChanged(nameof(HayError));
            OnPropertyChanged(nameof(EstaVacio));
            OnPropertyChanged(nameof(SinRegistros));
        }
    }

    /// <summary>FAB "+": abre la Lista de Sorteos (mockup 6.1).</summary>
    [RelayCommand]
    private Task AgregarAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(nameof(Views.ListaSorteosDevolucionPage));
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
