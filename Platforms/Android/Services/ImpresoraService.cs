using System.Text;
using MiCachito.Mobile.Helpers;
using MiCachito.Mobile.Services;

namespace MiCachito.Mobile.Platforms.Android.Services;

/// <summary>
/// Implementación Android del flujo de impresión Bluetooth (F6).
///
/// - Permiso runtime BLUETOOTH_CONNECT (Android 12+) y dispositivos
///   VINCULADOS: el emparejamiento se hace desde Ajustes de Android
///   (flujo del SO); la app LISTA los vinculados y conecta vía SPP.
/// - Conexión: BluetoothSocket RFCOMM/SPP al UUID estándar
///   00001101-0000-1000-8000-00805F9B34FB (Serial Port Profile) con
///   fallback createRfcommSocket (canal 5) para impresoras que no
///   publican el UUID SPP.
/// - Impresión: texto ESC/POS puro (init, centrado, bold, cortes de
///   línea, feed y corte parcial) — sin librerías externas.
/// - Persistencia: la impresora seleccionada (MAC) se guarda en
///   Preferences (configuración de dispositivo, no dato de negocio).
///
/// NOTA: este namespace (…Platforms.Android…) oculta los namespaces
/// Android.* — TODO acceso a la API Android va cualificado con
/// global::.
/// </summary>
public class ImpresoraService : IImpresoraService
{
    private const string PermisoConnect = "android.permission.BLUETOOTH_CONNECT";

    /// <summary>UUID estándar del Serial Port Profile (SPP).</summary>
    private static readonly global::Java.Util.UUID UuidSpp =
        global::Java.Util.UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");

    /// <summary>
    /// Respaldos de impresoras térmicas Bluetooth comunes que usan
    /// canal RFCOMM fijo en lugar de/SPP además del UUID SPP (algunas
    /// clones no publican el registro SDP del UUID).
    /// </summary>
    private static readonly int[] CanalesFallback = [5, 4, 3, 6, 1];

    // ── Estado del contrato viejo (mockup 5.1 / Cuenta) ──────────────

    public Task<string> ImprimirAsync(string rutaArchivo, string titulo)
    {
        // Contrato fase interfaz (DetalleVenta): reporta el estado de la
        // impresión Bluetooth sin enviar PDF (la impresión real de tickets
        // de Gestión sigue siendo trabajo de integración de ese módulo).
        var comprobante = "Comprobante de venta";
        return Task.FromResult(comprobante + " guardado en: " + rutaArchivo);
    }

    public Task<IReadOnlyList<string>> ObtenerDispositivosEnlazadosAsync()
    {
        return Task.Run<IReadOnlyList<string>>(() =>
        {
            var dispositivos = ObtenerVinculados();
            return dispositivos
                .Select(d => d.Name ?? d.Address)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .OrderBy(n => n)
                .Cast<string>()
                .ToList();
        });
    }

    // ── Contrato F6: dispositivos, selección, impresión ESC/POS ──────

    public Task<IReadOnlyList<DispositivoBluetooth>> ObtenerDispositivosAsync()
    {
        return Task.Run(() => (IReadOnlyList<DispositivoBluetooth>)ObtenerVinculados()
            .Select(d => new DispositivoBluetooth
            {
                Direccion = d.Address ?? string.Empty,
                Nombre = d.Name,
            })
            .Where(d => !string.IsNullOrWhiteSpace(d.Direccion))
            .ToList());
    }

    public void SeleccionarImpresora(string direccionMac)
    {
        if (string.IsNullOrWhiteSpace(direccionMac))
        {
            global::Microsoft.Maui.Storage.Preferences.Default.Remove(
                global::MiCachito.Mobile.Helpers.StorageKeys.ImpresoraBluetooth);
            return;
        }
        global::Microsoft.Maui.Storage.Preferences.Default.Set(
            global::MiCachito.Mobile.Helpers.StorageKeys.ImpresoraBluetooth, direccionMac);
    }

    public string? ImpresoraSeleccionada()
    {
        return global::Microsoft.Maui.Storage.Preferences.Default.Get<string?>(
            global::MiCachito.Mobile.Helpers.StorageKeys.ImpresoraBluetooth, null);
    }

    public async Task<ResultadoImpresion> ImprimirComprobanteAsync(ComprobanteLotenal comprobante)
    {
        try
        {
            // 1) Permiso runtime BLUETOOTH_CONNECT (Android 12+)
            string? problema = ValidarPermisoYAdapter();
            if (problema is not null)
            {
                return ResultadoImpresion.Fallo(problema, requiereSeleccion: false);
            }

            // 2) Resolver el dispositivo destino
            global::Android.Bluetooth.BluetoothDevice? destino = ResolverDestino();
            if (destino is null)
            {
                return ResultadoImpresion.Fallo(
                    "No hay impresora seleccionada. Selecciona una impresora Bluetooth e inténtalo de nuevo.",
                    requiereSeleccion: true);
            }

            // 3) Conectar SPP + enviar ESC/POS
            string ticket = ArmarticketEscPos(comprobante);

            string errorCon = await Task.Run(() => Enviar(destino, ticket)).ConfigureAwait(false);
            if (errorCon is not null)
            {
                return ResultadoImpresion.Fallo(errorCon, requiereSeleccion: true);
            }

            return ResultadoImpresion.Ok(
                $"Comprobante impreso en {destino.Name ?? destino.Address}");
        }
        catch (global::Java.IO.IOException ex)
        {
            return ResultadoImpresion.Fallo(
                "No se pudo conectar con la impresora. Verifica que esté encendida y cerca, e inténtalo de nuevo.",
                requiereSeleccion: true);
        }
        catch (Exception ex)
        {
            return ResultadoImpresion.Fallo(
                "Error de impresión: " + ex.Message, requiereSeleccion: true);
        }
    }

    // ── Helpers permiso / adapter / dispositivos ─────────────────────

    /// <summary>Null si todo OK; mensaje de error si falta permiso o BT apagado.</summary>
    private static string? ValidarPermisoYAdapter()
    {
        var ctx = (global::Android.Content.Context)global::Android.App.Application.Context;

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            if (ctx.CheckSelfPermission(PermisoConnect)
                != global::Android.Content.PM.Permission.Granted)
            {
                return "Se necesita el permiso de Bluetooth para imprimir. "
                    + "Concede el permiso en Ajustes e inténtalo de nuevo.";
            }
        }

        var manager = ctx.GetSystemService(
            global::Android.Content.Context.BluetoothService)
            as global::Android.Bluetooth.BluetoothManager;
        global::Android.Bluetooth.BluetoothAdapter? adapter = manager?.Adapter;

        if (adapter is null || !adapter.IsEnabled)
        {
            return "El Bluetooth está apagado. Enciéndelo para imprimir.";
        }

        return null;
        }

    /// <summary>Vinculados con acceso seguro (permiso concedido).</summary>
    private static List<global::Android.Bluetooth.BluetoothDevice> ObtenerVinculados()
    {
        var ctx = (global::Android.Content.Context)global::Android.App.Application.Context;

        if (OperatingSystem.IsAndroidVersionAtLeast(31)
            && ctx.CheckSelfPermission(PermisoConnect)
                != global::Android.Content.PM.Permission.Granted)
        {
            return [];
        }

        var manager = ctx.GetSystemService(
            global::Android.Content.Context.BluetoothService)
            as global::Android.Bluetooth.BluetoothManager;
        global::Android.Bluetooth.BluetoothAdapter? adapter = manager?.Adapter;

        if (adapter is null || !adapter.IsEnabled)
        {
            return [];
        }

        try
        {
            return [.. adapter.BondedDevices];
        }
        catch (global::Java.Lang.SecurityException)
        {
            return [];
        }
    }

    /// <summary>
    /// Impresora destino: la SELECCIONADA persistida; si no hay,
    /// la única vinculada; null si ninguna.
    /// </summary>
    private global::Android.Bluetooth.BluetoothDevice? ResolverDestino()
    {
        string? mac = ImpresoraSeleccionada();
        var vinculados = ObtenerVinculados();

        if (!string.IsNullOrWhiteSpace(mac))
        {
            global::Android.Bluetooth.BluetoothDevice? elegida = vinculados
                .FirstOrDefault(d => d.Address == mac);
            if (elegida is not null)
            {
                return elegida;
            }
        }

        return vinculados.Count == 1 ? vinculados[0] : null;
    }

    // ── Conexión SPP + envío ─────────────────────────────────────────

    /// <summary>
    /// Conecta SPP (con fallback de canal) y escribe el ticket.
    /// Null = éxito; mensaje = error para la UI.
    /// </summary>
    private static string? Enviar(
        global::Android.Bluetooth.BluetoothDevice destino, string ticket)
    {
        global::Android.Bluetooth.BluetoothSocket? socket = null;
        try
        {
            socket = Conectar(destino);

            using global::System.IO.Stream salida = socket.OutputStream;
            byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes(ticket);
            salida.Write(bytes, 0, bytes.Length);
            salida.Flush();
            return null;
        }
        catch (global::Java.IO.IOException)
        {
            return "No se pudo conectar con la impresora. Verifica que esté encendida y cerca, e inténtalo de nuevo.";
        }
        finally
        {
            try { socket?.Close(); } catch { /* cierre best-effort */ }
        }
    }

    /// <summary>
    /// Abre el socket SPP: UUID estándar primero; fallback createRfcomm
    /// a canales comunes de impresoras térmicas que no publican SPP.
    /// </summary>
    private static global::Android.Bluetooth.BluetoothSocket Conectar(
        global::Android.Bluetooth.BluetoothDevice destino)
    {
        Exception ultima = new global::Java.IO.IOException("Sin conexión intentada");

        // 1) UUID SPP estándar (servicio serial Bluetooth de la impresora)
        try
        {
            var s = destino.CreateRfcommSocketToServiceRecord(UuidSpp);
            s.Connect();
            return s;
        }
        catch (Exception exUuid)
        {
            ultima = exUuid;
        }

        // 2) Fallback: reintento INSEGURO del mismo UUID SPP (algunas
        // impresoras/clones solo aceptan el canal insecure del SPP).
        try
        {
            var s = destino.CreateInsecureRfcommSocketToServiceRecord(UuidSpp);
            s.Connect();
            return s;
        }
        catch (Exception exInsecure)
        {
            ultima = exInsecure;
        }

        throw ultima;
    }

    // ── ESC/POS: armado del ticket ───────────────────────────────────

    /// <summary>
    /// Ticket ESC/POS de texto puro (58 mm ≈ 32 columnas): init,
    /// centrado+bold para encabezado, columnas normales para el
    /// desglose, total en bold, feed y corte parcial.
    /// </summary>
    private static string ArmarticketEscPos(ComprobanteLotenal c)
    {
        const int ancho = 32;
        var sb = new StringBuilder();

        // ESC @ init
        sb.Append("\x1B@");
        // ESC a 1 centrado
        sb.Append("\x1Ba\x01");
        // ESC E 1 bold on
        sb.Append("\x1BE\x01");
        sb.Append("MIA CACHITO\n");
        sb.Append(c.Tipo.ToUpperInvariant() + "\n");
        // ESC E 0 bold off
        sb.Append("\x1BE\x00");
        sb.Append("Loteria Nacional\n\n");

        // ESC a 0 izquierda
        sb.Append("\x1Ba\x00");
        sb.Append(Fila("Folio", c.Folio, ancho));
        sb.Append(Fila("Fecha", c.FechaHora, ancho));
        sb.Append(Fila("Billetero", c.Billetero, ancho));
        sb.Append(Fila("Sorteo", c.ProductoSorteo, ancho));
        sb.Append('\n');

        foreach (string linea in c.Lineas)
        {
            sb.Append(TruncarCentrar(linea, ancho) + "\n");
        }

        sb.Append('\n');
        sb.Append("\x1BE\x01");
        sb.Append(Fila("TOTAL", c.Total, ancho));
        sb.Append("\x1BE\x00");
        sb.Append("\nGRACIAS POR SU COMPRA\n");

        // Feed 3 líneas + corte parcial
        sb.Append("\x1Bd\x03");
        sb.Append("\x1DVF\x00");

        return sb.ToString();
    }

    /// <summary>"etiqueta............ valor" al ancho del papel.</summary>
    private static string Fila(string etiqueta, string valor, int ancho)
    {
        string e = etiqueta.Length > ancho / 2 ? etiqueta[..(ancho / 2)] : etiqueta;
        int resto = Math.Max(1, ancho - e.Length);
        return e + new string('.', Math.Min(2, resto)) + valor.PadLeft(Math.Max(0, ancho - e.Length - 2)).TrimEnd();
    }

    /// <summary>Línea libre centrada (desglose), truncada al ancho.</summary>
    private static string TruncarCentrar(string linea, int ancho)
    {
        if (linea.Length <= ancho)
        {
            int izq = (ancho - linea.Length) / 2;
            return new string(' ', izq) + linea;
        }
        return linea[..ancho];
    }
}
