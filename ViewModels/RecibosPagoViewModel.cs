using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Entities;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// VM de la lista "Recibos de Pago" (mockup 8): cards con Folio, Fecha,
/// Total y check verde de confirmación, en orden cronológico inverso.
///
/// Conectado al backend real (GET api/mobile/pagos/recibos): solo fichas
/// APLICADAS por Caja. Sin bloque de crédito: los mockups 8.x no lo
/// muestran (verificado 2026-09-21 contra las 5 imágenes de referencia).
///
/// Estados: cargando (overlay), vacío, sin conexión/HTTP (mensaje, se
/// conservan los datos ya cargados si los hay).
/// </summary>
public partial class RecibosPagoViewModel : BaseViewModel
{
    private readonly RecibosPagoService _recibos;

    /// <summary>Recibos en orden cronológico inverso.</summary>
    public ObservableCollection<ReciboPagoItemApi> Recibos { get; } = new();

    [ObservableProperty]
    private bool cargando;

    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>True con recibos cargados (oculta el estado vacío).</summary>
    [ObservableProperty]
    private bool hayRecibos;

    /// <summary>True cuando hay mensaje de error (muestra el aviso rojo).</summary>
    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    /// <summary>True cuando la carga terminó sin recibos (estado vacío).</summary>
    public bool EstaVacio => !Cargando && !HayError && Recibos.Count == 0;

    /// <summary>Mensaje plano del estado vacío ("No hay recibos...").</summary>
    [ObservableProperty]
    private string mensajeVacio = "No hay recibos de pago aplicados todavía";

    public RecibosPagoViewModel(RecibosPagoService recibos)
    {
        _recibos = recibos;
        Title = "Recibos de Pago";
    }

    /// <summary>Carga la lista y el crédito al entrar (y al volver del detalle).</summary>
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
                List<ReciboPagoItemApi> items = await _recibos.CargarRecibosAsync();
                Recibos.Clear();
                foreach (ReciboPagoItemApi r in items)
                {
                    Recibos.Add(r);
                }
                HayRecibos = Recibos.Count > 0;
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
                MensajeError = $"No se pudieron cargar los recibos: {ex.Message}";
            }
        }
        finally
        {
            Cargando = false;
            // Derivadas notificadas al FINAL (con Cargando ya en false):
            // notificarlas dentro del try dejaba EstaVacio siempre en
            // false y el estado vacío jamás aparecía (auditoría 2026-09-21).
            OnPropertyChanged(nameof(HayError));
            OnPropertyChanged(nameof(EstaVacio));
        }
    }

    /// <summary>Abre el detalle del recibo seleccionado (por id_ficha_pago).</summary>
    [RelayCommand]
    private async Task SeleccionarReciboAsync(ReciboPagoItemApi? recibo)
    {
        if (recibo is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(
            $"{nameof(Views.DetallePagoPage)}?folio={recibo.IdFichaPago}",
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
