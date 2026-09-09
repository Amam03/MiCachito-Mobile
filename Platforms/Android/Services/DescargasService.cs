using Android.App;
using Android.Content;
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
            Activity? activity = Platform.CurrentActivity;
            if (activity is null || activity.IsFinishing || activity.IsDestroyed)
            {
                return null;
            }

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
