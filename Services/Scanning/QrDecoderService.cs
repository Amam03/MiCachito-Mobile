using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace MiCachito.Mobile.Services.Scanning;

/// <summary>
/// Decodifica codigos de barras/QR de los boletos.
/// - Decodificar(Stream): desde una FOTO (binding ZXing.SkiaSharp, sin
///   dependencia de MAUI: compatible con el entorno net10.0-rc2).
/// - DecodificarYuv(byte[],int,int): desde el plano Y (luminancia) de un
///   frame EN VIVO de la camara integrada (CameraX ImageAnalysis entrega
///   YUV_420_888; el plano Y ya es la luminancia que ZXing espera),
///   usando PlanarYUVLuminanceSource del core de ZXing.Net.
/// </summary>
public sealed class QrDecoderService
{
    /// <summary>Formatos aceptados en fotos: QR de los boletos + 1D alternativos.</summary>
    private static readonly BarcodeFormat[] FormatosAceptados =
    {
        BarcodeFormat.QR_CODE,
        BarcodeFormat.CODE_128,
        BarcodeFormat.EAN_13,
        BarcodeFormat.DATA_MATRIX,
        BarcodeFormat.PDF_417,
    };

    /// <summary>QR unicamente para el escaneo en vivo (frames a ~15 fps).</summary>
    private static readonly BarcodeFormat[] FormatosEnVivo =
    {
        BarcodeFormat.QR_CODE,
    };

    /// <summary>
    /// Decodifica la foto (stream de imagen) y retorna el texto del primer
    /// codigo encontrado, o null si la foto no contiene ningun codigo legible.
    /// </summary>
    public string? Decodificar(Stream foto)
    {
        using SKBitmap? bitmap = SKBitmap.Decode(foto);
        if (bitmap is null)
        {
            return null;
        }

        var reader = new ZXing.SkiaSharp.BarcodeReader
        {
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = FormatosAceptados,
            },
        };

        Result? resultado = reader.Decode(bitmap);
        return resultado?.Text;
    }

    /// <summary>
    /// Decodifica un QR desde el plano Y de un frame de camara en vivo.
    /// Estatico y puro: lo invoca el analyzer del handler de camara sin DI.
    /// </summary>
    public static string? DecodificarYuv(byte[] planoY, int ancho, int alto)
    {
        var source = new PlanarYUVLuminanceSource(planoY, ancho, alto, 0, 0, ancho, alto, false);
        var reader = new BarcodeReaderGeneric
        {
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = FormatosEnVivo,
            },
        };

        return reader.Decode(source)?.Text;
    }
}
