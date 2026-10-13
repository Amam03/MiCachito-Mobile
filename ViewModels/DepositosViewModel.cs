using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM del listado "Depósitos" (mockup 7): cards con Folio (DEP-...),
/// Captura, "BANCO - $monto" y FechaHora + nube ámbar cuando el estatus
/// es 'pendiente' (aún sin validar por Caja), en orden cronológico
/// inverso + FAB "+" hacia Nuevo Depósito.
///
/// Conectado al backend real (GET api/mobile/depositos): historial del
/// billetero de la sesión. Estados: cargando (overlay), vacío, error con
/// reintento por re-entrada (patrón Recibos de Pago).
/// </summary>
public partial class DepositosViewModel : BaseViewModel
{
    private readonly DepositoService _depositos;

    /// <summary>Depósitos del billetero (más reciente primero).</summary>
    public ObservableCollection<DepositoApi> Registros { get; } = new();

    /// <summary>True mientras corre el GET del historial (overlay).</summary>
    [ObservableProperty]
    private bool cargando;

    /// <summary>Mensaje de error de la carga (reintentar re-entrando).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True cuando hay mensaje de error (muestra el aviso rojo).</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>True cuando la carga terminó sin depósitos (estado vacío).</summary>
    public bool EstaVacio => !Cargando && !HayError && Registros.Count == 0;

    /// <summary>Mensaje del estado vacío.</summary>
    [ObservableProperty]
    private string mensajeVacio = "No hay depósitos registrados";

    public DepositosViewModel(DepositoService depositos)
    {
        _depositos = depositos;
        Title = "Depósitos";
    }

    /// <summary>Carga el historial real al entrar (y al volver de Nuevo Depósito).</summary>
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
                List<DepositoApi> items = await _depositos.CargarDepositosAsync();
                Registros.Clear();
                foreach (DepositoApi d in items)
                {
                    Registros.Add(d);
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
                MensajeError = $"No se pudieron cargar los depósitos: {ex.Message}";
            }
        }
        finally
        {
            Cargando = false;
            // Derivadas notificadas al FINAL (con Cargando ya en false):
            // notificarlas dentro del try dejaba EstaVacio siempre en
            // false y el estado vacío jamás aparecía (patrón Recibos).
            OnPropertyChanged(nameof(HayError));
            OnPropertyChanged(nameof(EstaVacio));
        }
    }

    /// <summary>FAB "+": abre Nuevo Depósito (mockup 7.1).</summary>
    [RelayCommand]
    private Task AgregarAsync()
    {
        if (Shell.Current is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync(nameof(Views.NuevoDepositoPage));
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
