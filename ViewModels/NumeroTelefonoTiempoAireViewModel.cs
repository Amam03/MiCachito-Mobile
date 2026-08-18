using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla de ingreso del número de teléfono para Tiempo Aire.
/// Recibe el Id del proveedor y el monto seleccionado vía QueryProperty (navegación Shell).
/// UI-only: el botón Finalizar no ejecuta una recarga real.
/// La validación de coincidencia entre campos se maneja aquí, no en la View.
/// </summary>
[QueryProperty(nameof(ProveedorIdStr), "proveedorId")]
[QueryProperty(nameof(MontoStr), "monto")]
public partial class NumeroTelefonoTiempoAireViewModel : BaseViewModel
{
    private ProveedorTiempoAire? _proveedor;

    /// <summary>
    /// Texto del encabezado (nombre del proveedor + subproducto).
    /// </summary>
    [ObservableProperty]
    private string _nombreProveedor = string.Empty;

    /// <summary>
    /// Color de fondo del encabezado (color de marca del proveedor).
    /// </summary>
    [ObservableProperty]
    private Color _colorHeader = Color.FromArgb("#3A21DC");

    /// <summary>
    /// Color del texto del encabezado.
    /// </summary>
    [ObservableProperty]
    private Color _textColorHeader = Colors.White;

    /// <summary>
    /// Monto seleccionado en la pantalla anterior.
    /// </summary>
    [ObservableProperty]
    private decimal _montoSeleccionado;

    /// <summary>
    /// Texto a mostrar del monto (ej. "$50").
    /// </summary>
    public string MontoDisplay => $"${MontoSeleccionado}";

    /// <summary>
    /// Primer campo: número de teléfono ingresado por el usuario.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FinalizarCommand))]
    private string _numeroTelefono = string.Empty;

    /// <summary>
    /// Segundo campo: confirmación del número de teléfono.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(FinalizarCommand))]
    private string _confirmarNumeroTelefono = string.Empty;

    /// <summary>
    /// Indica si se debe mostrar la palomita verde (ambos números coinciden y tienen 10 dígitos).
    /// </summary>
    [ObservableProperty]
    private bool _numerosCoinciden;

    /// <summary>
    /// Indica si se debe mostrar el mensaje de error en rojo (los números no coinciden).
    /// </summary>
    [ObservableProperty]
    private bool _numerosNoCoinciden;

    /// <summary>
    /// Indica si el botón Finalizar está habilitado.
    /// Requiere que ambos campos tengan 10 dígitos y coincidan.
    /// </summary>
    public bool PuedeFinalizar =>
        NumerosCoinciden
        && !string.IsNullOrWhiteSpace(NumeroTelefono)
        && NumeroTelefono.Trim().Length >= 10;

    /// <summary>
    /// Id del proveedor recibido vía navegación Shell (como string).
    /// </summary>
    public string? ProveedorIdStr
    {
        get => _proveedor?.Id.ToString();
        set
        {
            if (int.TryParse(value, out var id))
            {
                CargarProveedor(id);
            }
        }
    }

    /// <summary>
    /// Monto recibido vía navegación Shell (como string).
    /// </summary>
    public string? MontoStr
    {
        get => MontoSeleccionado.ToString();
        set
        {
            if (decimal.TryParse(value, out var monto))
            {
                MontoSeleccionado = monto;
                OnPropertyChanged(nameof(MontoDisplay));
            }
        }
    }

    /// <summary>
    /// Carga los datos del proveedor desde TiempoAireData.
    /// </summary>
    private void CargarProveedor(int id)
    {
        _proveedor = TiempoAireData.ObtenerPorId(id);
        if (_proveedor is null)
        {
            return;
        }

        NombreProveedor = _proveedor.DisplayText;
        ColorHeader = _proveedor.ColorTarjeta;
        TextColorHeader = _proveedor.TextColor;
        Title = NombreProveedor;
    }

    /// <summary>
    /// Se ejecuta automáticamente cuando cambia NumeroTelefono.
    /// Actualiza el estado de validación visual.
    /// </summary>
    partial void OnNumeroTelefonoChanged(string value)
    {
        ActualizarValidacion();
    }

    /// <summary>
    /// Se ejecuta automáticamente cuando cambia ConfirmarNumeroTelefono.
    /// Actualiza el estado de validación visual.
    /// </summary>
    partial void OnConfirmarNumeroTelefonoChanged(string value)
    {
        ActualizarValidacion();
    }

    /// <summary>
    /// Evalúa la coincidencia entre los dos campos y actualiza los flags visuales.
    /// Solo muestra validación cuando el segundo campo tiene contenido.
    /// </summary>
    private void ActualizarValidacion()
    {
        // Solo validar si el campo de confirmación tiene algo escrito
        if (string.IsNullOrWhiteSpace(ConfirmarNumeroTelefono))
        {
            NumerosCoinciden = false;
            NumerosNoCoinciden = false;
            return;
        }

        var num1 = (NumeroTelefono ?? string.Empty).Trim();
        var num2 = ConfirmarNumeroTelefono.Trim();

        if (num1 == num2 && num1.Length >= 10)
        {
            NumerosCoinciden = true;
            NumerosNoCoinciden = false;
        }
        else if (num1 != num2)
        {
            NumerosCoinciden = false;
            NumerosNoCoinciden = true;
        }
        else
        {
            // Coinciden pero no tienen 10 dígitos aún
            NumerosCoinciden = false;
            NumerosNoCoinciden = false;
        }
    }

    /// <summary>
    /// Botón Finalizar. UI-only: no ejecuta recarga real.
    /// Retrocede a la pantalla de proveedores tras "procesar".
    /// </summary>
    [RelayCommand(CanExecute = nameof(PuedeFinalizar))]
    private async Task FinalizarAsync()
    {
        // TODO: Implementar recarga real cuando exista el endpoint del backend.
        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync("../../../TiempoAirePage");
        }
    }

    /// <summary>
    /// Retrocede a la pantalla anterior (montos del proveedor).
    /// </summary>
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
