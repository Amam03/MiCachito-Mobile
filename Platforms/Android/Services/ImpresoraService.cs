using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.Platforms.Android.Services;

/// <summary>
/// Implementación Android del flujo de impresión Bluetooth (mockup
/// 5.1/5.2). Fase SOLO INTERFAZ: verifica permisos (BLUETOOTH_CONNECT
/// en Android 12+) e impresoras vinculadas; si NO hay impresora
/// vinculada devuelve el aviso (caso emulador). La impresión real
/// (SPP + ESC/POS) se documenta en docs/NOTAS_TICKETS_VENTA.md para
/// la fase de integración.
///
/// NOTA: este namespace (…Platforms.Android…) oculta los namespaces
/// Android.* — TODO acceso a la API Android va cualificado con
/// global::.
/// </summary>
public class ImpresoraService : IImpresoraService
{
    private const string PermisoConnect = "android.permission.BLUETOOTH_CONNECT";

    public Task<string> ImprimirAsync(string rutaArchivo, string titulo)
    {
        var ctx = (global::Android.Content.Context)global::Android.App.Application.Context;

        // 1) Permiso runtime BLUETOOTH_CONNECT (Android 12+)
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            if (ctx.CheckSelfPermission(PermisoConnect)
                != global::Android.Content.PM.Permission.Granted)
            {
                return Task.FromResult(
                    "Se necesita el permiso de Bluetooth para imprimir. "
                    + "Concede el permiso en Ajustes e inténtalo de nuevo.");
            }
        }

        // 2) Adapter y dispositivos vinculados
        var manager = ctx.GetSystemService(
            global::Android.Content.Context.BluetoothService)
            as global::Android.Bluetooth.BluetoothManager;
        global::Android.Bluetooth.BluetoothAdapter? adapter = manager?.Adapter;

        if (adapter is null || !adapter.IsEnabled)
        {
            return Task.FromResult("El Bluetooth está apagado. Enciéndelo para imprimir.");
        }

        global::System.Collections.Generic.ICollection<global::Android.Bluetooth.BluetoothDevice>? vinculados;
        try
        {
            vinculados = adapter.BondedDevices;
        }
        catch (global::Java.Lang.SecurityException)
        {
            return Task.FromResult(
                "Se necesita el permiso de Bluetooth para imprimir. "
                + "Concede el permiso e inténtalo de nuevo.");
        }

        if (vinculados is null || vinculados.Count == 0)
        {
            return Task.FromResult(
                "No hay ninguna impresora vinculada. Vincula una impresora "
                + "Bluetooth en los ajustes del teléfono e inténtalo de nuevo.");
        }

        // 3) Hay vinculadas — la impresión real es trabajo de integración
        string nombres = string.Join(", ", vinculados.Select(d => d.Name ?? d.Address));
        return Task.FromResult(
            $"Impresoras vinculadas: {nombres}. El envío Bluetooth real se "
            + "implementará con la integración del backend.");
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ObtenerDispositivosEnlazadosAsync()
    {
        var ctx = (global::Android.Content.Context)global::Android.App.Application.Context;

        // 1) Permiso runtime BLUETOOTH_CONNECT (Android 12+)
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            if (ctx.CheckSelfPermission(PermisoConnect)
                != global::Android.Content.PM.Permission.Granted)
            {
                return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }
        }

        // 2) Adapter y dispositivos vinculados
        var manager = ctx.GetSystemService(
            global::Android.Content.Context.BluetoothService)
            as global::Android.Bluetooth.BluetoothManager;
        global::Android.Bluetooth.BluetoothAdapter? adapter = manager?.Adapter;

        if (adapter is null || !adapter.IsEnabled)
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        global::System.Collections.Generic.ICollection<global::Android.Bluetooth.BluetoothDevice>? vinculados;
        try
        {
            vinculados = adapter.BondedDevices;
        }
        catch (global::Java.Lang.SecurityException)
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        if (vinculados is null || vinculados.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var nombres = vinculados
            .Select(d => d.Name ?? d.Address)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .OrderBy(n => n)
            .Cast<string>()
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(nombres);
    }
}
