using Microsoft.Maui.Graphics;

namespace MiCachito.Mobile.Models.Entities;

/// <summary>
/// DTO de un proveedor de tiempo aire para la pantalla de selección.
/// Fecha: 2026-08-14. Persona: UI-only — sin conexión a backend todavía.
/// </summary>
public class ProveedorTiempoAire
{
    public int Id { get; set; }

    /// <summary>
    /// Nombre del proveedor (ej. Telcel, AT&amp;T, Movistar, Bait).
    /// </summary>
    public string NombreProveedor { get; set; } = string.Empty;

    /// <summary>
    /// Sub-producto del proveedor (ej. "Recarga", "Paquete", "Internet").
    /// Puede ser null cuando el proveedor no tiene sub-productos.
    /// </summary>
    public string? Subproducto { get; set; }

    /// <summary>
    /// Texto a mostrar en la tarjeta.
    /// Si hay subproducto, muestra "NombreProveedor Subproducto"
    /// (ej. "Telcel RECARGA", "Bait Internet en Casa", "VALOR TELECOM CASA").
    /// Si no hay subproducto, muestra solo NombreProveedor (ej. "AT&amp;T", "Movistar").
    /// </summary>
    public string DisplayText =>
        string.IsNullOrWhiteSpace(Subproducto)
            ? NombreProveedor
            : $"{NombreProveedor} {Subproducto}";

    /// <summary>
    /// Color de fondo de la tarjeta (color de marca del proveedor).
    /// </summary>
    public Color ColorTarjeta { get; set; } = Color.FromArgb("#002F7A");

    /// <summary>
    /// Nombre del recurso de imagen del logo (sin extensión).
    /// </summary>
    public string LogoIcon { get; set; } = "logo_telcel";

    /// <summary>
    /// Color del texto sobre la tarjeta.
    /// </summary>
    public Color TextColor { get; set; } = Colors.White;

    /// <summary>
    /// Ancho del logo en la tarjeta.
    /// </summary>
    public double LogoWidth { get; set; } = 80;

    /// <summary>
    /// Alto del logo en la tarjeta.
    /// </summary>
    public double LogoHeight { get; set; } = 32;

    /// <summary>
    /// Lista de montos disponibles para este proveedor/subproducto.
    /// Se llena desde Data/TiempoAireData.cs (estático, UI-only).
    /// </summary>
    public IReadOnlyList<decimal> Montos { get; set; } = Array.Empty<decimal>();
}