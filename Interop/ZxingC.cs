using System;
using System.Runtime.InteropServices;

namespace MiCachito.Mobile.Interop;

/// <summary>Formatos de imagen que acepta ZXing_ImageView_new (ZXingC.h).</summary>
internal enum ImageFormatZxing : uint
{
    None = 0,
    /// <summary>Luminancia 8-bit por pixel (el plano Y de YUV_420_888).</summary>
    Lum = 0x01000000,
}

/// <summary>BarcodeFormat de zxing-cpp: ZX_BCF_ID(SYM, VAR) = SYM | (VAR &lt;&lt; 8).</summary>
internal enum BarcodeFormatZxing : int
{
    /// <summary>QRCode = 'Q'(0x51) | ' '(0x20)&lt;&lt;8 (BarcodeFormat.h linea 66).</summary>
    QRCode = 0x2051,
}

/// <summary>
/// Wrapper C# de la C API de zxing-cpp (libzxingc.so, compilada para
/// arm64-v8a y x86_64 con el NDK 27.1 desde el tag v3.1.1).
/// zxing-cpp decodifica los QRs REALES de los boletos (34/35 digitos,
/// modulos densos) que ZXing.Net 0.16.9 NO logra leer (verificado contra
/// los renders de los 3 PDFs: 2/7 crops con ZXing.Net vs 7/7 con zxing-cpp).
/// Referencia: core/src/ZXingC.h del tag v3.1.1.
/// </summary>
internal static partial class ZxingC
{
    private const string Lib = "zxingc";

    // === ImageView (la data la retiene el caller: no la libera la lib) ===

    [LibraryImport(Lib, EntryPoint = "ZXing_ImageView_new_checked")]
    internal static partial IntPtr ImageViewNew(byte[] data, int size, int width, int height, ImageFormatZxing format, int rowStride, int pixStride);

    [LibraryImport(Lib, EntryPoint = "ZXing_ImageView_delete")]
    internal static partial void ImageViewDelete(IntPtr iv);

    [LibraryImport(Lib, EntryPoint = "ZXing_ImageView_rotate")]
    internal static partial void ImageViewRotate(IntPtr iv, int degree);

    // === ReaderOptions ===

    [LibraryImport(Lib, EntryPoint = "ZXing_ReaderOptions_new")]
    internal static partial IntPtr ReaderOptionsNew();

    [LibraryImport(Lib, EntryPoint = "ZXing_ReaderOptions_delete")]
    internal static partial void ReaderOptionsDelete(IntPtr opts);

    [LibraryImport(Lib, EntryPoint = "ZXing_ReaderOptions_setTryHarder")]
    internal static partial void SetTryHarder(IntPtr opts, [MarshalAs(UnmanagedType.U1)] bool tryHarder);

    [LibraryImport(Lib, EntryPoint = "ZXing_ReaderOptions_setTryRotate")]
    internal static partial void SetTryRotate(IntPtr opts, [MarshalAs(UnmanagedType.U1)] bool tryRotate);

    [LibraryImport(Lib, EntryPoint = "ZXing_ReaderOptions_setTryInvert")]
    internal static partial void SetTryInvert(IntPtr opts, [MarshalAs(UnmanagedType.U1)] bool tryInvert);

    [LibraryImport(Lib, EntryPoint = "ZXing_ReaderOptions_setFormats")]
    internal static partial void SetFormats(IntPtr opts, BarcodeFormatZxing[] formats, int count);

    // === ReadBarcodes + resultado ===

    [LibraryImport(Lib, EntryPoint = "ZXing_ReadBarcodes")]
    internal static partial IntPtr ReadBarcodes(IntPtr iv, IntPtr opts);

    [LibraryImport(Lib, EntryPoint = "ZXing_Barcodes_size")]
    internal static partial int BarcodesSize(IntPtr barcodes);

    [LibraryImport(Lib, EntryPoint = "ZXing_Barcodes_at")]
    internal static partial IntPtr BarcodesAt(IntPtr barcodes, int i);

    [LibraryImport(Lib, EntryPoint = "ZXing_Barcodes_delete")]
    internal static partial void BarcodesDelete(IntPtr barcodes);

    [LibraryImport(Lib, EntryPoint = "ZXing_Barcode_isValid")]
    [return: MarshalAs(UnmanagedType.U1)]
    internal static partial bool BarcodeIsValid(IntPtr barcode);

    [LibraryImport(Lib, EntryPoint = "ZXing_Barcode_text")]
    internal static partial IntPtr BarcodeText(IntPtr barcode);

    [LibraryImport(Lib, EntryPoint = "ZXing_free")]
    internal static partial void Free(IntPtr ptr);

    /// <summary>
    /// Lee el primer QR valido desde un plano de luminancia 8-bit (plano Y
    /// de un frame YUV_420_888 de CameraX). grados = RotationDegrees del
    /// frame informado por CameraX (se compensa antes de decodificar).
    /// Retorna el texto del QR o null si no hay ninguno legible.
    /// </summary>
    internal static string? LeerQr(byte[] planoY, int ancho, int alto, int grados)
    {
        IntPtr iv = IntPtr.Zero;
        IntPtr opts = IntPtr.Zero;
        IntPtr barcodes = IntPtr.Zero;
        try
        {
            iv = ImageViewNew(planoY, planoY.Length, ancho, alto, ImageFormatZxing.Lum, ancho, 1);
            if (grados is 90 or 180 or 270)
            {
                ImageViewRotate(iv, grados);
            }

            opts = ReaderOptionsNew();
            SetTryHarder(opts, true);
            SetTryRotate(opts, true);
            SetTryInvert(opts, true);
            SetFormats(opts, new[] { BarcodeFormatZxing.QRCode }, 1);

            barcodes = ReadBarcodes(iv, opts);
            if (barcodes == IntPtr.Zero)
            {
                return null;
            }

            int n = BarcodesSize(barcodes);
            for (int i = 0; i < n; i++)
            {
                IntPtr bc = BarcodesAt(barcodes, i);
                if (bc == IntPtr.Zero || !BarcodeIsValid(bc))
                {
                    continue;
                }

                IntPtr pTexto = BarcodeText(bc);
                if (pTexto == IntPtr.Zero)
                {
                    continue;
                }

                string? texto = Marshal.PtrToStringUTF8(pTexto);
                Free(pTexto);
                if (!string.IsNullOrEmpty(texto))
                {
                    return texto;
                }
            }

            return null;
        }
        finally
        {
            if (barcodes != IntPtr.Zero)
            {
                BarcodesDelete(barcodes);
            }
            if (opts != IntPtr.Zero)
            {
                ReaderOptionsDelete(opts);
            }
            if (iv != IntPtr.Zero)
            {
                ImageViewDelete(iv);
            }
        }
    }
}
