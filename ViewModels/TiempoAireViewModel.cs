using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.ViewModels;

/// <summary>
/// ViewModel de la pantalla de selección de proveedor de Tiempo Aire.
/// Fecha: 2026-08-14. Persona: UI-only — sin conexión a backend todavía.
/// Los proveedores son estáticos; cuando exista el endpoint se reemplazarán
/// por datos del backend.
/// </summary>
public partial class TiempoAireViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<ProveedorTiempoAire> _proveedores = new();

    public TiempoAireViewModel()
    {
        Title = "Tiempo Aire";
        CargarProveedores();
    }

    private void CargarProveedores()
    {
        // Datos estáticos para la interfaz.
        // TODO: Reemplazar con datos del backend cuando exista el endpoint.
        var lista = new List<ProveedorTiempoAire>
        {
            // --- Telcel ---
            new()
            {
                Id = 1,
                NombreProveedor = "Telcel",
                Subproducto = "Recarga",
                ColorTarjeta = Color.FromArgb("#002F7A"),
                LogoIcon = "logo_telcel",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },
            new()
            {
                Id = 2,
                NombreProveedor = "Telcel",
                Subproducto = "Paquete",
                ColorTarjeta = Color.FromArgb("#0A2A6B"),
                LogoIcon = "logo_telcel",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },
            new()
            {
                Id = 3,
                NombreProveedor = "Telcel",
                Subproducto = "Internet",
                ColorTarjeta = Color.FromArgb("#0A3D91"),
                LogoIcon = "logo_telcel",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },
            new()
            {
                Id = 4,
                NombreProveedor = "Telcel",
                Subproducto = "X Tiempo",
                ColorTarjeta = Color.FromArgb("#1B46B2"),
                LogoIcon = "logo_telcel",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },

            // --- AT&T ---
            new()
            {
                Id = 5,
                NombreProveedor = "AT&T",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#00ACDF"),
                LogoIcon = "logo_att",
                TextColor = Colors.White,
                LogoWidth = 44,
                LogoHeight = 44,
            },

            // --- Movistar ---
            new()
            {
                Id = 6,
                NombreProveedor = "Movistar",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#019DF4"),
                LogoIcon = "logo_movistar",
                TextColor = Colors.White,
                LogoWidth = 90,
                LogoHeight = 28,
            },

            // --- Bait ---
            new()
            {
                Id = 7,
                NombreProveedor = "Bait",
                Subproducto = "Recarga",
                ColorTarjeta = Color.FromArgb("#FFCC00"),
                LogoIcon = "logo_bait",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 60,
                LogoHeight = 28,
            },
            new()
            {
                Id = 8,
                NombreProveedor = "Bait",
                Subproducto = "Paquetes",
                ColorTarjeta = Color.FromArgb("#FFB81C"),
                LogoIcon = "logo_bait",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 60,
                LogoHeight = 28,
            },
            new()
            {
                Id = 9,
                NombreProveedor = "Bait",
                Subproducto = "Internet",
                ColorTarjeta = Color.FromArgb("#E8930C"),
                LogoIcon = "logo_bait",
                TextColor = Colors.White,
                LogoWidth = 60,
                LogoHeight = 28,
            },
            new()
            {
                Id = 10,
                NombreProveedor = "Bait",
                Subproducto = "Internet en Casa",
                ColorTarjeta = Color.FromArgb("#D67D00"),
                LogoIcon = "logo_bait",
                TextColor = Colors.White,
                LogoWidth = 60,
                LogoHeight = 28,
            },

            // ===================================================
            // --- Proveedores de la imagen 3.2 ---
            // ===================================================

            // --- CFE Internet Bienestar ---
            new()
            {
                Id = 11,
                NombreProveedor = "CFE Internet Bienestar",
                Subproducto = "Internet",
                ColorTarjeta = Color.FromArgb("#6B002C"),
                LogoIcon = "logo_cfe_bienestar",
                TextColor = Colors.White,
                LogoWidth = 90,
                LogoHeight = 40,
            },

            // --- Soriana Móvil (sin sub-producto) ---
            new()
            {
                Id = 12,
                NombreProveedor = "Soriana Móvil",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#651181"),
                LogoIcon = "logo_soriana",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },

            // --- Soriana Móvil Paquetes ---
            new()
            {
                Id = 13,
                NombreProveedor = "Soriana Móvil",
                Subproducto = "Paquetes",
                ColorTarjeta = Color.FromArgb("#022D5A"),
                LogoIcon = "logo_soriana",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },

            // --- Unefon ---
            new()
            {
                Id = 14,
                NombreProveedor = "Unefon",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#58AC6E"),
                LogoIcon = "logo_unefon",
                TextColor = Colors.White,
                LogoWidth = 70,
                LogoHeight = 32,
            },

            // --- Cierto ---
            new()
            {
                Id = 15,
                NombreProveedor = "Cierto",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#FDECBC"),
                LogoIcon = "logo_cierto",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 60,
                LogoHeight = 32,
            },

            // ===================================================
            // --- Proveedores de la imagen 3.3 ---
            // ===================================================

            // --- Flash Mobile / FreedomPop ---
            new()
            {
                Id = 16,
                NombreProveedor = "Flash Mobile",
                Subproducto = "FreedomPop",
                ColorTarjeta = Color.FromArgb("#47CCEA"),
                LogoIcon = "logo_flash",
                TextColor = Colors.White,
                LogoWidth = 110,
                LogoHeight = 32,
            },

            // --- Netwey / Mi Móvil ---
            new()
            {
                Id = 17,
                NombreProveedor = "Netwey",
                Subproducto = "Mi Móvil",
                ColorTarjeta = Color.FromArgb("#774A9F"),
                LogoIcon = "logo_netwey",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 28,
            },

            // --- Yobicel ---
            new()
            {
                Id = 18,
                NombreProveedor = "Yobicel",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#983D8D"),
                LogoIcon = "logo_yobicel",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },

            // --- Ultracel / Telmovil ---
            new()
            {
                Id = 19,
                NombreProveedor = "Ultracel",
                Subproducto = "Telmovil",
                ColorTarjeta = Color.FromArgb("#5A0082"),
                LogoIcon = "logo_ultracel",
                TextColor = Colors.White,
                LogoWidth = 100,
                LogoHeight = 40,
            },

            // ===================================================
            // --- Proveedores de la imagen 3.4 ---
            // ===================================================

            // --- Valor Telecom Casa ---
            new()
            {
                Id = 20,
                NombreProveedor = "Valor Telecom",
                Subproducto = "Casa",
                ColorTarjeta = Color.FromArgb("#D4D5D4"),
                LogoIcon = "logo_valor_casa",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 80,
                LogoHeight = 32,
            },

            // --- Valor Telecom Paquetes ---
            new()
            {
                Id = 21,
                NombreProveedor = "Valor Telecom",
                Subproducto = "Paquetes",
                ColorTarjeta = Color.FromArgb("#203665"),
                LogoIcon = "logo_valor_paquetes",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },

            // --- weex / Macropay ---
            new()
            {
                Id = 22,
                NombreProveedor = "weex",
                Subproducto = "Macropay",
                ColorTarjeta = Color.FromArgb("#534789"),
                LogoIcon = "logo_weex",
                TextColor = Colors.White,
                LogoWidth = 60,
                LogoHeight = 28,
            },

            // --- rediCoppel ---
            new()
            {
                Id = 23,
                NombreProveedor = "rediCoppel",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#F0D224"),
                LogoIcon = "logo_redicoppel",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 90,
                LogoHeight = 28,
            },
        };

        Proveedores = new ObservableCollection<ProveedorTiempoAire>(lista);
    }

    /// <summary>
    /// Prepara la selección visual del proveedor. No implementa la recarga todavía.
    /// </summary>
    [RelayCommand]
    private Task SeleccionarProveedorAsync(ProveedorTiempoAire proveedor)
    {
        if (proveedor is null)
        {
            return Task.CompletedTask;
        }

        // Selección visual únicamente — sin navegación ni recarga.
        return Task.CompletedTask;
    }
}