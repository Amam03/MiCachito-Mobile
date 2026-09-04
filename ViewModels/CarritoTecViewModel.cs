using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Data;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel del carrito Tec (pantalla 14), a la que se llega desde el
/// carrito del header de la 12/13.
///
/// Regla clave (spec usuario): CADA BILLETE ES UN REGISTRO INDIVIDUAL
/// (vs LOTENAL 11, que agrupa por tienda).
///
/// Reglas:
///   - Eliminar: quita el registro y la pleca verde de la fila de origen
///     (la MISMA instancia del catálogo, por eso la 12/13 lo reflejan
///     sin recargar).
///   - Vender: abre la pantalla 15 (Datos del Cliente) para confirmar
///     la venta; el carrito se limpia tras Confirmar Venta.
///   - Barra inferior (estilo pulido de la 11): Cantidad = número de
///     billetes, Total = suma de precios.
/// </summary>
public partial class CarritoTecViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<RegistroCarritoTec> _registros = new();

    public CarritoTecViewModel()
    {
        Title = "Carrito de Compras";
        CargarRegistros();
    }

    // ============ Derivados (notificar con NotificarDerivadas) ============

    /// <summary>Suma de billetes del carrito.</summary>
    public int CantidadBilletes => Registros.Count;

    /// <summary>Suma de importes.</summary>
    public decimal TotalImporte => Registros.Sum(r => r.Total);

    /// <summary>Línea "Cantidad" de la barra inferior.</summary>
    public string CantidadTotalTexto => $"Cantidad: {CantidadBilletes}";

    /// <summary>Línea "Total" de la barra inferior.</summary>
    public string TotalTexto => $"Total: ${TotalImporte:0.00}";

    /// <summary>Badge del carrito del header.</summary>
    public string BadgeTexto => Registros.Count.ToString();

    /// <summary>True con al menos un registro (oculta el estado vacío).</summary>
    public bool HayRegistros => Registros.Count > 0;

    /// <summary>True sin registros (muestra el mensaje de carrito vacío).</summary>
    public bool SinRegistros => Registros.Count == 0;

    /// <summary>
    /// Reconstruye los registros desde el catálogo compartido: los
    /// billetes con Agregado=true de cualquier sorteo.
    /// </summary>
    private void CargarRegistros()
    {
        var registros = SorteosTecData.BilletesAgregados()
            .Select(CrearRegistro)
            .ToList();

        Registros = new ObservableCollection<RegistroCarritoTec>(registros);
    }

    /// <summary>
    /// Construye el registro de un billete: número, sorteo y precio
    /// individual = precio del sorteo TAL CUAL (SorteoTec.Precio, misma
    /// regla que la 11 pulida: no se divide ni se inventa otra fuente).
    /// </summary>
    private RegistroCarritoTec CrearRegistro(BilleteTec billete)
    {
        var sorteo = SorteosTecData.ObtenerPorId(billete.IdSorteo);

        return new RegistroCarritoTec
        {
            IdBillete = billete.IdBillete,
            NumeroBillete = billete.Numero,
            SorteoTexto = sorteo?.NombreSorteo ?? string.Empty,
            Precio = sorteo?.Precio ?? 0m,
            ColorHex = sorteo?.ColorHex ?? string.Empty
        };
    }

    /// <summary>
    /// Al volver de la 15 (OnAppearing): reconstruye los registros
    /// (Confirmar Venta vació el carrito).
    /// </summary>
    public void AlAparecer()
    {
        CargarRegistros();
        NotificarDerivadas();
    }

    /// <summary>
    /// Botón Eliminar de un registro: lo quita del carrito y la fila de
    /// origen pierde la pleca verde (misma instancia del catálogo).
    /// </summary>
    [RelayCommand]
    private void EliminarRegistro(RegistroCarritoTec? registro)
    {
        if (registro is null)
        {
            return;
        }

        var billete = SorteosTecData.BilletesAgregados()
            .FirstOrDefault(b => b.IdBillete == registro.IdBillete);

        if (billete is not null)
        {
            billete.Agregado = false;
        }

        Registros.Remove(registro);
        NotificarDerivadas();
    }

    /// <summary>
    /// Botón Vender: abre la pantalla 15 (Datos del Cliente).
    /// </summary>
    [RelayCommand]
    private async Task VenderAsync()
    {
        if (IsBusy || Registros.Count == 0)
        {
            return;
        }

        IsBusy = true;

        try
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(nameof(Views.DatosClienteTecPage));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Retrocede a la pantalla anterior.
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

    private void NotificarDerivadas()
    {
        OnPropertyChanged(nameof(CantidadBilletes));
        OnPropertyChanged(nameof(TotalImporte));
        OnPropertyChanged(nameof(CantidadTotalTexto));
        OnPropertyChanged(nameof(TotalTexto));
        OnPropertyChanged(nameof(BadgeTexto));
        OnPropertyChanged(nameof(HayRegistros));
        OnPropertyChanged(nameof(SinRegistros));
    }
}
