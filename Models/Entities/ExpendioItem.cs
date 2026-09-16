using CommunityToolkit.Mvvm.ComponentModel;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// Expendio (credencial) del conjunto del billetero autenticado — item de
/// GET api/mobile/expendios. Se muestra en las tarjetas de Administración
/// y en el modal de Consulta (mockups Expendios 1-6).
///
/// La identidad (usuario/titular/domicilio) es ADMINISTRADA por Desktop
/// (cont-cedis/clientes): en Mobile es SOLO LECTURA. Los Permisos de Venta
/// no viven aquí: son compartidos a nivel billetero (BilleteroMobile).
/// </summary>
public partial class ExpendioItem : ObservableObject
{
    /// <summary>Identificador de la credencial (billeteros_expendios.id_expendio).</summary>
    public int IdExpendio { get; set; }

    /// <summary>Nombre de usuario de la credencial (alias de la tarjeta).</summary>
    [ObservableProperty]
    private string _usuario = string.Empty;

    /// <summary>Titular del conjunto (billeteros.nombre_completo).</summary>
    [ObservableProperty]
    private string _titular = string.Empty;

    /// <summary>Domicilio legible del JSON billeteros.direccion (o vacío).</summary>
    [ObservableProperty]
    private string _domicilio = string.Empty;

    /// <summary>Credencial autorizada para la app (billeteros_expendios.autorizado).</summary>
    [ObservableProperty]
    private bool _autorizado = true;

    /// <summary>
    /// Marcado en el modal "Seleccionar Fechas y Expendios" (consulta de
    /// registros). Estado de UI sobre la instancia compartida de la lista.
    /// </summary>
    [ObservableProperty]
    private bool _seleccionadoConsulta = true;

    /// <summary>Negación de <see cref="SeleccionadoConsulta"/> (checkbox vacío, sin converters).</summary>
    public bool NoSeleccionadoConsulta => !SeleccionadoConsulta;

    partial void OnSeleccionadoConsultaChanged(bool value)
    {
        OnPropertyChanged(nameof(NoSeleccionadoConsulta));
    }
}
