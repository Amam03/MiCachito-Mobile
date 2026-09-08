using System.Windows.Input;

namespace MiCachito.Mobile.Controls;

/// <summary>
/// Vista de camara INTEGRADA para escanear el QR de los boletos.
/// Renderiza el preview en vivo (CameraX en Android) dentro de la propia
/// pantalla de Mi Cachito, sin abrir aplicaciones externas de camara.
/// El QR detectado se entrega via DetectionCommand con el texto decodificado.
/// </summary>
public class CameraScannerView : View
{
    /// <summary>Comando que recibe el texto del QR detectado (string).</summary>
    public static readonly BindableProperty DetectionCommandProperty =
        BindableProperty.Create(nameof(DetectionCommand), typeof(ICommand), typeof(CameraScannerView));

    /// <summary>True = camara encendida escaneando; false = detenida.</summary>
    public static readonly BindableProperty IsScanningProperty =
        BindableProperty.Create(nameof(IsScanning), typeof(bool), typeof(CameraScannerView), false);

    /// <summary>True = linterna encendida (solo aplica con camara activa).</summary>
    public static readonly BindableProperty TorchOnProperty =
        BindableProperty.Create(nameof(TorchOn), typeof(bool), typeof(CameraScannerView), false);

    /// <summary>
    /// Pulso true = decodificar el frame ACTUAL ya (boton Capturar QR).
    /// Two-way: el handler lo regresa a false al consumir el intento.
    /// </summary>
    public static readonly BindableProperty ForzarCapturaProperty =
        BindableProperty.Create(nameof(ForzarCaptura), typeof(bool), typeof(CameraScannerView), false,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Comando que se dispara cuando una captura forzada no encuentra ningun codigo.</summary>
    public static readonly BindableProperty NoDetectadoCommandProperty =
        BindableProperty.Create(nameof(NoDetectadoCommand), typeof(ICommand), typeof(CameraScannerView));

    public ICommand? DetectionCommand
    {
        get => (ICommand?)GetValue(DetectionCommandProperty);
        set => SetValue(DetectionCommandProperty, value);
    }

    public bool IsScanning
    {
        get => (bool)GetValue(IsScanningProperty);
        set => SetValue(IsScanningProperty, value);
    }

    public bool TorchOn
    {
        get => (bool)GetValue(TorchOnProperty);
        set => SetValue(TorchOnProperty, value);
    }

    /// <summary>Pulso: decodificar el frame actual sin esperar el debounce.</summary>
    public bool ForzarCaptura
    {
        get => (bool)GetValue(ForzarCapturaProperty);
        set => SetValue(ForzarCapturaProperty, value);
    }

    public ICommand? NoDetectadoCommand
    {
        get => (ICommand?)GetValue(NoDetectadoCommandProperty);
        set => SetValue(NoDetectadoCommandProperty, value);
    }

    /// <summary>Llamado por el handler cuando el escaner detecta un codigo en vivo.</summary>
    internal void RaiseDetection(string texto)
    {
        if (DetectionCommand?.CanExecute(texto) == true)
        {
            DetectionCommand.Execute(texto);
        }
    }

    /// <summary>Llamado por el handler para fijar el estado real de la linterna.</summary>
    internal void RaiseTorchDisponible(bool disponible)
    {
        TorchDisponible = disponible;
    }

    /// <summary>Llamado por el handler cuando una captura forzada no detecto nada.</summary>
    internal void RaiseNoDetectado()
    {
        if (NoDetectadoCommand?.CanExecute(null) == true)
        {
            NoDetectadoCommand.Execute(null);
        }
    }

    /// <summary>True si la camara activa tiene flash disponible (solo lectura, interno).</summary>
    public static readonly BindableProperty TorchDisponibleProperty =
        BindableProperty.Create(nameof(TorchDisponible), typeof(bool), typeof(CameraScannerView), false);

    public bool TorchDisponible
    {
        get => (bool)GetValue(TorchDisponibleProperty);
        set => SetValue(TorchDisponibleProperty, value);
    }
}
