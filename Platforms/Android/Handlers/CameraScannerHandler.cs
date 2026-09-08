#if ANDROID
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Lifecycle;
using Google.Common.Util.Concurrent;
using Java.Lang;
using Microsoft.Maui.Handlers;
using MiCachito.Mobile.Controls;
using ContextCompat = global::AndroidX.Core.Content.ContextCompat;
using AndroidContext = global::Android.Content.Context;
using MauiPlatform = global::Microsoft.Maui.ApplicationModel.Platform;

namespace MiCachito.Mobile.Platforms.Android.Handlers;

/// <summary>
/// Handler Android de CameraScannerView: embebe un PreviewView de CameraX
/// dentro de la pagina de consulta (camara INTEGRADA, sin apps externas) y
/// decodifica frames en vivo con zxing-cpp (plano Y de YUV_420_888).
/// Arranque/paro dirigidos por IsScanning (bind de CamaraLista del VM):
/// la camara solo se abre cuando el permiso ya fue concedido, y se libera
/// al salir de la pantalla (unbindAll).
/// </summary>
public class CameraScannerHandler : ViewHandler<CameraScannerView, PreviewView>
{
    public static readonly PropertyMapper<CameraScannerView, CameraScannerHandler> Mapper =
        new(ViewHandler.ViewMapper);

    private ProcessCameraProvider? _provider;
    private ICamera? _camera;
    private ImageAnalysis? _analysis;
    private Preview? _preview;
    private AnalyzerZxing? _analyzer;
    private bool _liberado;

    public CameraScannerHandler() : base(Mapper)
    {
    }

    protected override PreviewView CreatePlatformView()
    {
        // defaults de PreviewView: FillCenter + Performance (TextureView)
        return new PreviewView(MauiContext?.Context);
    }

    protected override void ConnectHandler(PreviewView platformView)
    {
        base.ConnectHandler(platformView);
        VirtualView.PropertyChanged += OnVirtualViewPropertyChanged;
        if (VirtualView.IsScanning)
        {
            ArrancarCamara();
        }
    }

    protected override void DisconnectHandler(PreviewView platformView)
    {
        // Invalidar callbacks asincronos EN VUELO (provider de la camara) ANTES
        // de soltar la vista virtual: si el runnable llega despues de aqui,
        // AlProviderListo ve _liberado=true y retorna sin tocar VirtualView
        // (cuyo getter LANTA InvalidOperationException al estar desconectado,
        // causa de crash al salir rapido del escaner mientras la camara arranca).
        _liberado = true;
        if (VirtualView is not null)
        {
            VirtualView.PropertyChanged -= OnVirtualViewPropertyChanged;
        }
        LiberarCamara();
        base.DisconnectHandler(platformView);
    }

    private void OnVirtualViewPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // sender puede ser null tras un DisconnectHandler defensivo; y el
        // getter VirtualView lanza si el handler ya se solto (guard barato).
        if (_liberado || sender is not CameraScannerView vista)
        {
            return;
        }

        if (e.PropertyName == CameraScannerView.IsScanningProperty.PropertyName)
        {
            if (VirtualView.IsScanning)
            {
                ArrancarCamara();
            }
            else
            {
                LiberarCamara();
            }
        }
        else if (e.PropertyName == CameraScannerView.TorchOnProperty.PropertyName)
        {
            AplicarLinterna();
        }
        else if (e.PropertyName == CameraScannerView.ForzarCapturaProperty.PropertyName)
        {
            if (VirtualView.ForzarCaptura)
            {
                // consumir el pulso YA: si la camara no esta activa el handler
                // lo regresa a false y el VM avisa al usuario.
                VirtualView.ForzarCaptura = false;
                if (_analyzer is not null)
                {
                    _analyzer.ForzarProximoFrame();
                }
                else
                {
                    _ = VirtualView.Dispatcher.DispatchAsync(() => VirtualView?.RaiseNoDetectado());
                }
            }
        }
    }

    private void ArrancarCamara()
    {
        if (_provider is not null || _liberado)
        {
            return; // ya arrancada o handler desconectado (callback invalido)
        }

        var context = MauiContext?.Context;
        if (context is null)
        {
            return;
        }

        IListenableFuture futuro = ProcessCameraProvider.GetInstance(context);
        futuro.AddListener(
            new ProviderRunnable(() => AlProviderListo(futuro, context)),
            ContextCompat.GetMainExecutor(context));
    }

    private void AlProviderListo(IListenableFuture futuro, AndroidContext context)
    {
        // El runnable puede llegar DESPUES de un DisconnectHandler (navegar
        // atras rapido o salir del escaner mientras la camara aun arranca):
        // _liberado=true invalida esta respuesta tardia. No tocar VirtualView
        // antes de este check: su getter lanza si el handler ya se solto.
        if (_liberado || _provider is not null)
        {
            return;
        }

        CameraScannerView? vista = null;
        try
        {
            vista = VirtualView; // lanza si el handler ya esta desconectado
        }
        catch (System.InvalidOperationException)
        {
            return; // handler desconectado entre el bind y este callback
        }

        if (vista is null || !vista.IsScanning)
        {
            return; // se solto mientras llegaba el provider
        }

        try
        {
            _provider = (ProcessCameraProvider)futuro.Get();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CameraScanner] provider: {ex.Message}");
            return;
        }

        _liberado = false;

        _analyzer = new AnalyzerZxing(
            texto =>
            {
                string t = texto;
                // capturar vista (no VirtualView): un frame ya en cola puede
                // ejecutarse tras DisconnectHandler y el getter lanzaria.
                _ = vista.Dispatcher.DispatchAsync(() => vista.RaiseDetection(t));
            },
            () =>
            {
                _ = vista.Dispatcher.DispatchAsync(() => vista.RaiseNoDetectado());
            });

        // defaults de ImageAnalysis: KEEP_ONLY_LATEST + ~640x480
        _analysis = new ImageAnalysis.Builder().Build();
        _analysis.SetAnalyzer(ContextCompat.GetMainExecutor(context), _analyzer);

        _preview = new Preview.Builder().Build();
        _preview.SetSurfaceProvider(
            ContextCompat.GetMainExecutor(context),
            PlatformView.SurfaceProvider);

        var lifecycleOwner = MauiPlatform.CurrentActivity as ILifecycleOwner;
        if (lifecycleOwner is null)
        {
            LiberarCamara();
            return;
        }

        _camera = _provider.BindToLifecycle(
            lifecycleOwner,
            CameraSelector.DefaultBackCamera,
            _preview,
            _analysis);

        AplicarLinterna();
    }

    private void AplicarLinterna()
    {
        try
        {
            if (_camera is not null && _camera.CameraControl is not null)
            {
                _ = _camera.CameraControl.EnableTorch(VirtualView.TorchOn);
            }
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CameraScanner] linterna: {ex.Message}");
        }
    }

    private void LiberarCamara()
    {
        if (_liberado)
        {
            return;
        }
        _liberado = true;
        try
        {
            _analysis?.ClearAnalyzer();
            _provider?.UnbindAll();
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CameraScanner] liberar: {ex.Message}");
        }
        finally
        {
            _provider = null;
            _camera = null;
            _analysis = null;
            _preview = null;
            _analyzer = null;
        }
    }

    /// <summary>Runnable que entrega el provider asincrono en el hilo principal.</summary>
    private sealed class ProviderRunnable : Java.Lang.Object, Java.Lang.IRunnable
    {
        private readonly Action _accion;

        public ProviderRunnable(Action accion)
        {
            _accion = accion;
        }

        public void Run()
        {
            _accion();
        }
    }

    /// <summary>Analyzer que decodifica el plano Y de cada frame con zxing-cpp.</summary>
    private sealed class AnalyzerZxing : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private readonly Action<string> _alDetectar;
        private readonly Action _alNoDetectar;
        private long _ultimoTs;
        private volatile bool _forzar;

        public AnalyzerZxing(Action<string> alDetectar, Action alNoDetectar)
        {
            _alDetectar = alDetectar;
            _alNoDetectar = alNoDetectar;
        }

        /// <summary>El boton Capturar QR pidio decodificar el proximo frame sin debounce.</summary>
        public void ForzarProximoFrame()
        {
            _forzar = true;
        }

        /// <summary>
        /// Requerido por el binding: CameraX 1.4 invoca esta propiedad al hacer
        /// bind (AbstractMethodError si no se implementa). null deja que
        /// ImageAnalysis use su default interno (~640x480).
        /// </summary>
        public global::Android.Util.Size? DefaultTargetResolution => null;

        /// <summary>Sistema de coordenadas del analisis (default: sensor).</summary>
        public int TargetCoordinateSystem => 0;

        public void Analyze(IImageProxy proxy)
        {
            try
            {
                // debounce ~400ms: la deteccion exitosa navega; no hace falta
                // decodificar absolutamente todos los frames
                long ts = JavaSystem.CurrentTimeMillis();
                bool forzado = _forzar;
                if (!forzado && ts - _ultimoTs < 400)
                {
                    return;
                }
                _ultimoTs = ts;
                _forzar = false;
                global::Android.Util.Log.Info("ZXINGC",
                    $"frame {proxy.Width}x{proxy.Height} rot={proxy.ImageInfo.RotationDegrees} stride={proxy.GetPlanes()[0].RowStride}" + (forzado ? " (forzado)" : ""));

                IImageProxyPlaneProxy planoY = proxy.GetPlanes()[0];
                using Java.Nio.ByteBuffer buffer = planoY.Buffer;
                int rowStride = planoY.RowStride;
                int ancho = proxy.Width;
                int alto = proxy.Height;

                byte[] crudo = new byte[buffer.Remaining()];
                buffer.Get(crudo);

                byte[] denso = rowStride == ancho
                    ? crudo
                    : CompactarStride(crudo, rowStride, ancho, alto);

                int grados = proxy.ImageInfo.RotationDegrees;
                string? texto = Interop.ZxingC.LeerQr(denso, ancho, alto, grados);
                if (!string.IsNullOrEmpty(texto))
                {
                    global::Android.Util.Log.Info("ZXINGC", "QR detectado: " + texto[..System.Math.Min(40, texto.Length)]);
                    _alDetectar(texto);
                }
                else if (forzado)
                {
                    // captura manual sin codigo: avisar al usuario
                    global::Android.Util.Log.Info("ZXINGC", "captura forzada sin QR");
                    _alNoDetectar();
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Analyzer] {ex.Message}");
            }
            finally
            {
                proxy.Close();
            }
        }

        private static byte[] CompactarStride(byte[] crudo, int rowStride, int ancho, int alto)
        {
            byte[] denso = new byte[ancho * alto];
            for (int fila = 0; fila < alto; fila++)
            {
                Array.Copy(crudo, fila * rowStride, denso, fila * ancho, ancho);
            }
            return denso;
        }
    }
}
#endif
