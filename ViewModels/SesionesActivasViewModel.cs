using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Api;
using MiCachito.Mobile.Models.Responses;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// Sesiones Activas (M-06, auditoría 2026-10-20). Lista los dispositivos con
/// sesión ACTIVA en el backend (GET api/mobile/auth/sesiones) del expendio
/// autenticado, con revocación individual por fila (POST
/// api/mobile/auth/sesiones/revocar, confirmación tipo modal Cerrar Sesión).
///
/// NO es la lista Bluetooth de DispositivosEnlazadosViewModel (impresoras
/// VINCULadas al teléfono): son las sesiones autenticadas del expendio.
/// La sesión ACTUAL (actual=true) NUNCA se puede revocar desde aquí (para
/// eso está Cerrar Sesión): la fila se muestra sin botón de revocar.
/// </summary>
public partial class SesionesActivasViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    /// <summary>Sesiones activas del expendio (fecha descendente, del backend).</summary>
    public ObservableCollection<SesionDispositivo> Sesiones { get; } = new();

    /// <summary>True mientras corre el GET (overlay Cargando).</summary>
    [ObservableProperty]
    private bool cargando;

    /// <summary>Mensaje de error de la carga (reintentar re-entrando).</summary>
    [ObservableProperty]
    private string mensajeError = string.Empty;

    /// <summary>Mensaje de error de una revocación (toast de fila, no aborta la lista).</summary>
    [ObservableProperty]
    private string mensajeRevocacion = string.Empty;

    /// <summary>Sesión en confirmación de revocación (modal), null si cerrado.</summary>
    [ObservableProperty]
    private SesionDispositivo? sesionPorRevocar;

    /// <summary>True cuando el modal de revocación está abierto (scrim+card).</summary>
    public bool ModalRevocacionVisible => SesionPorRevocar is not null;

    /// <summary>Nombre del dispositivo en confirmación ("" si cerrado).</summary>
    public string SesionPorRevocarDispositivo => SesionPorRevocar?.Dispositivo ?? string.Empty;

    partial void OnSesionPorRevocarChanged(SesionDispositivo? value)
    {
        OnPropertyChanged(nameof(ModalRevocacionVisible));
        OnPropertyChanged(nameof(SesionPorRevocarDispositivo));
    }

    public bool HayError => !string.IsNullOrEmpty(MensajeError);

    public bool EstaVacio => !Cargando && !HayError && Sesiones.Count == 0;

    public bool HayMensajeRevocacion => !string.IsNullOrEmpty(MensajeRevocacion);

    [ObservableProperty]
    private string mensajeVacio = "No hay sesiones activas";

    public SesionesActivasViewModel(IAuthService authService)
    {
        _authService = authService;
        Title = "Sesiones Activas";
    }

    /// <summary>
    /// Carga las sesiones activas reales al entrar (y al volver de otra
    /// pantalla). Patrón AlAparecer de PremiosReintegros/Depósitos.
    /// </summary>
    public async Task AlAparecerAsync()
    {
        if (Cargando)
        {
            return;
        }

        Cargando = true;
        MensajeError = string.Empty;
        MensajeRevocacion = string.Empty;
        try
        {
            try
            {
                var sesiones = await _authService.SesionesAsync();
                Sesiones.Clear();
                foreach (var s in sesiones ?? new List<SesionDispositivo>())
                {
                    Sesiones.Add(s);
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
                MensajeError = $"No se pudieron cargar las sesiones: {ex.Message}";
            }
        }
        finally
        {
            Cargando = false;
            // Derivadas al FINAL (patrón PremiosReintegros): notificarlas
            // dentro del try dejaba EstaVacio siempre en false.
            OnPropertyChanged(nameof(Sesiones));
            OnPropertyChanged(nameof(HayError));
            OnPropertyChanged(nameof(EstaVacio));
        }
    }

    /// <summary>Abre el modal de confirmación para revocar una sesión ajena.</summary>
    [RelayCommand]
    private void ConfirmarRevocacion(SesionDispositivo sesion)
    {
        if (Cargando || Revocando)
        {
            return;
        }

        MensajeRevocacion = string.Empty;
        SesionPorRevocar = sesion;
    }

    /// <summary>Cancelar: cierra el modal sin revocar.</summary>
    [RelayCommand]
    private void CancelarRevocacion()
    {
        SesionPorRevocar = null;
    }

    /// <summary>
    /// Aceptar: revoca la sesión en el backend y recarga la lista. La sesión
    /// ACTUAL nunca llega aquí (la fila no muestra botón; guard defensivo).
    /// </summary>
    [RelayCommand]
    private async Task AceptarRevocacionAsync()
    {
        if (Revocando || SesionPorRevocar is null || SesionPorRevocar.Actual)
        {
            SesionPorRevocar = null;
            return;
        }

        Revocando = true;
        try
        {
            try
            {
                await _authService.RevocarSesionAsync(SesionPorRevocar.IdSesion);
                SesionPorRevocar = null;
                MensajeRevocacion = string.Empty;
                await AlAparecerAsync();
            }
            catch (TaskCanceledException)
            {
                MensajeRevocacion = "Sin conexión al servidor. Intenta de nuevo";
            }
            catch (OperationCanceledException)
            {
                MensajeRevocacion = "Operación cancelada";
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException)
            {
                MensajeRevocacion = "Sin conexión al servidor. Intenta de nuevo";
            }
            catch (ApiException ex)
            {
                MensajeRevocacion = $"No se pudo revocar la sesión: {ex.Message}";
                // 404 = sesión ajena o ya revocada: recargar para reflejar el
                // estado real del expendio.
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await AlAparecerAsync();
                }
            }
        }
        finally
        {
            Revocando = false;
        }
    }

    /// <summary>True mientras corre el POST de revocación (deshabilita acciones).</summary>
    [ObservableProperty]
    private bool revocando;

    /// <summary>Flecha atrás del header: pop relativo a Cuenta.</summary>
    [RelayCommand]
    private async Task VolverAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
