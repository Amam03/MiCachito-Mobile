using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Java.IO;
using Java.Lang;
using Environment = Android.OS.Environment;

namespace MiCachito.Mobile.Platforms.Android.Services;

/// <summary>
/// Guarda el PDF del comprobante en la carpeta pública Descargas vía
/// MediaStore (Android 10+ / API 29: sin permiso de escritura). Fase
/// solo-interfaz; el nombre visible es "pago-{folio}-{fecha}.pdf"
/// (mockup 8.3). Si falla, devuelve null y el llamador usa
/// AppDataDirectory como respaldo.
/// </summary>
public static class DescargasService
{
    /// <summary>Copia el archivo a Descargas y devuelve la ruta de visualización.</summary>
    public static string? GuardarEnDescargas(string rutaOrigen, string nombreVisible)
    {
        try
        {
            // MediaStore.Downloads + RelativePath exigen API 29+ (Android 10).
            // En API < 29 el guard devuelve null: el llamador ya usa
            // AppDataDirectory como respaldo (el PDF se abre igual desde el
            // visor). Soporte directo a Descargas en Android 9-: se cierra en
            // la fase de integración backend (WRITE_EXTERNAL_STORAGE).
            if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
            {
                return null;
            }

            Activity? activity = Platform.CurrentActivity;
            if (activity is null || activity.IsFinishing || activity.IsDestroyed)
            {
                return null;
            }

            // Sitios de llamada API 29+: protegidos por el guard de arriba.
#pragma warning disable CA1416
            var valores = new ContentValues();
            valores.Put(MediaStore.IMediaColumns.DisplayName, nombreVisible);
            valores.Put(MediaStore.IMediaColumns.MimeType, "application/pdf");
            valores.Put(MediaStore.IMediaColumns.RelativePath, "Download");

            global::Android.Net.Uri? uri = activity.ContentResolver?.Insert(
                MediaStore.Downloads.ExternalContentUri, valores);
            if (uri is null)
            {
                return null;
            }

            using Stream entrada = System.IO.File.OpenRead(rutaOrigen);
            using Stream? salida = activity.ContentResolver?.OpenOutputStream(uri);
            if (salida is null)
            {
                return null;
            }
#pragma warning restore CA1416

            entrada.CopyTo(salida);
            salida.Flush();

            return nombreVisible;
        }
        catch
        {
            return null;
        }
    }
}
