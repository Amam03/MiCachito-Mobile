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
        //
        // 34 proveedores en una sola lista vertical con scroll.
        // Los que repiten marca (Telcel, Bait, Soriana, VALOR TELECOM)
        // son entradas SEPARADAS, cada una con su subproducto.
        var lista = new List<ProveedorTiempoAire>
        {
            // ===================================================
            // --- Imagen 3.1 (1-10): Telcel ×4, AT&T, Movistar, Bait ×4
            // ===================================================

            // --- Telcel RECARGA ---
            new()
            {
                Id = 1,
                NombreProveedor = "Telcel",
                Subproducto = "RECARGA",
                ColorTarjeta = Color.FromArgb("#254AA6"),
                LogoIcon = "logo_telcel",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },
            // --- Telcel PAQUETE ---
            new()
            {
                Id = 2,
                NombreProveedor = "Telcel",
                Subproducto = "PAQUETE",
                ColorTarjeta = Color.FromArgb("#254AA6"),
                LogoIcon = "logo_telcel",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },
            // --- Telcel INTERNET ---
            new()
            {
                Id = 3,
                NombreProveedor = "Telcel",
                Subproducto = "INTERNET",
                ColorTarjeta = Color.FromArgb("#254AA6"),
                LogoIcon = "logo_telcel",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },
            // --- Telcel X TIEMPO ---
            new()
            {
                Id = 4,
                NombreProveedor = "Telcel",
                Subproducto = "X TIEMPO",
                ColorTarjeta = Color.FromArgb("#254AA6"),
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
                ColorTarjeta = Color.FromArgb("#00A7E2"),
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
                ColorTarjeta = Color.FromArgb("#1F5B7D"),
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
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#FFD43A"),
                LogoIcon = "logo_bait",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 60,
                LogoHeight = 28,
            },
            // --- Bait Internet ---
            new()
            {
                Id = 8,
                NombreProveedor = "Bait",
                Subproducto = "Internet",
                ColorTarjeta = Color.FromArgb("#FFD43A"),
                LogoIcon = "logo_bait",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 60,
                LogoHeight = 28,
            },
            // --- Bait Internet en Casa ---
            new()
            {
                Id = 9,
                NombreProveedor = "Bait",
                Subproducto = "Internet en Casa",
                ColorTarjeta = Color.FromArgb("#FFD43A"),
                LogoIcon = "logo_bait",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 60,
                LogoHeight = 28,
            },
            // --- Bait Paquetes ---
            new()
            {
                Id = 10,
                NombreProveedor = "Bait",
                Subproducto = "Paquetes",
                ColorTarjeta = Color.FromArgb("#FFD43A"),
                LogoIcon = "logo_bait",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 60,
                LogoHeight = 28,
            },

            // ===================================================
            // --- Imagen 3.2 (11-20): Internet Bienestar, CFE, Virquin,
            //     Soriana ×2, DIRY, OUI, PILLO FON, Cierto, ComparT fon
            // ===================================================

            // --- Internet para el Bienestar ---
            new()
            {
                Id = 11,
                NombreProveedor = "Internet para el Bienestar",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#6B002C"),
                LogoIcon = "logo_internet_bienestar",
                TextColor = Colors.White,
                LogoWidth = 110,
                LogoHeight = 40,
            },

            // --- CFE Internet ---
            new()
            {
                Id = 12,
                NombreProveedor = "CFE Internet",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#F2F2F2"),
                LogoIcon = "logo_cfe_internet",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 90,
                LogoHeight = 32,
            },

            // --- Virquin Mobile ---
            new()
            {
                Id = 13,
                NombreProveedor = "Virquin Mobile",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#FF0000"),
                LogoIcon = "logo_virquin_mobile",
                TextColor = Colors.White,
                LogoWidth = 100,
                LogoHeight = 32,
            },

            // --- Soriana Movil ---
            new()
            {
                Id = 14,
                NombreProveedor = "Soriana Movil",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#651181"),
                LogoIcon = "logo_soriana",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },
            // --- Soriana Movil Paquetes ---
            new()
            {
                Id = 15,
                NombreProveedor = "Soriana Movil",
                Subproducto = "Paquetes",
                ColorTarjeta = Color.FromArgb("#651181"),
                LogoIcon = "logo_soriana",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },

            // --- DIRY Móvil ---
            new()
            {
                Id = 16,
                NombreProveedor = "DIRY Móvil",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#022D5A"),
                LogoIcon = "logo_diry_movil",
                TextColor = Colors.White,
                LogoWidth = 90,
                LogoHeight = 32,
            },

            // --- OUI ---
            new()
            {
                Id = 17,
                NombreProveedor = "OUI",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#F2F2F2"),
                LogoIcon = "logo_oui",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 60,
                LogoHeight = 32,
            },

            // --- PILLO FON ---
            new()
            {
                Id = 18,
                NombreProveedor = "PILLO FON",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#58AC6E"),
                LogoIcon = "logo_pillo_fon",
                TextColor = Colors.White,
                LogoWidth = 90,
                LogoHeight = 32,
            },

            // --- Cierto ---
            new()
            {
                Id = 19,
                NombreProveedor = "Cierto",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#201A1A"),
                LogoIcon = "logo_cierto",
                TextColor = Colors.White,
                LogoWidth = 70,
                LogoHeight = 32,
            },

            // --- ComparT fon ---
            new()
            {
                Id = 20,
                NombreProveedor = "ComparT fon",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#CE0059"),
                LogoIcon = "logo_compart_fon",
                TextColor = Colors.White,
                LogoWidth = 90,
                LogoHeight = 32,
            },

            // ===================================================
            // --- Imagen 3.3 (21-30): Flash MOBILE, FreedomPop, Mi Movil,
            //     netwey, Yobi, rin cel, Ultracel, VALOR TELECOM ×3
            // ===================================================

            // --- Flash MOBILE ---
            new()
            {
                Id = 21,
                NombreProveedor = "Flash MOBILE",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#55C1E8"),
                LogoIcon = "logo_flash",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 110,
                LogoHeight = 32,
            },

            // --- FreedomPop ---
            new()
            {
                Id = 22,
                NombreProveedor = "FreedomPop",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#00A5EA"),
                LogoIcon = "logo_freedompop",
                TextColor = Colors.White,
                LogoWidth = 100,
                LogoHeight = 32,
            },

            // --- Mi Movil ---
            new()
            {
                Id = 23,
                NombreProveedor = "Mi Movil",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#774A9F"),
                LogoIcon = "logo_mi_movil",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },

            // --- netwey ---
            new()
            {
                Id = 24,
                NombreProveedor = "netwey",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#010101"),
                LogoIcon = "logo_netwey",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 28,
            },

            // --- Yobi ---
            new()
            {
                Id = 25,
                NombreProveedor = "Yobi",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#983D8D"),
                LogoIcon = "logo_yobi",
                TextColor = Colors.White,
                LogoWidth = 70,
                LogoHeight = 32,
            },

            // --- rin cel ---
            new()
            {
                Id = 26,
                NombreProveedor = "rin cel",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#1C78BB"),
                LogoIcon = "logo_rin_cel",
                TextColor = Colors.White,
                LogoWidth = 80,
                LogoHeight = 32,
            },

            // --- Ultracel ---
            new()
            {
                Id = 27,
                NombreProveedor = "Ultracel",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#00D8C4"),
                LogoIcon = "logo_ultracel",
                TextColor = Color.FromArgb("#1A1A1A"),
                LogoWidth = 100,
                LogoHeight = 32,
            },

            // --- VALOR TELECOM ---
            new()
            {
                Id = 28,
                NombreProveedor = "VALOR TELECOM",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#6D1BAB"),
                LogoIcon = "logo_valor_telecom",
                TextColor = Colors.White,
                LogoWidth = 95,
                LogoHeight = 32,
            },
            // --- VALOR TELECOM CASA ---
            new()
            {
                Id = 29,
                NombreProveedor = "VALOR TELECOM",
                Subproducto = "CASA",
                ColorTarjeta = Color.FromArgb("#6D1BAB"),
                LogoIcon = "logo_valor_casa",
                TextColor = Colors.White,
                LogoWidth = 90,
                LogoHeight = 32,
            },
            // --- VALOR TELECOM PAQUETES ---
            new()
            {
                Id = 30,
                NombreProveedor = "VALOR TELECOM",
                Subproducto = "PAQUETES",
                ColorTarjeta = Color.FromArgb("#6D1BAB"),
                LogoIcon = "logo_valor_paquetes",
                TextColor = Colors.White,
                LogoWidth = 90,
                LogoHeight = 32,
            },

            // ===================================================
            // --- Imagen 3.4 (31-34): Wimo telecom, Weex,
            //     Chip Macropay, redi Coppel
            // ===================================================

            // --- Wimo telecom ---
            new()
            {
                Id = 31,
                NombreProveedor = "Wimo telecom",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#925EA2"),
                LogoIcon = "logo_wimo_telecom",
                TextColor = Colors.White,
                LogoWidth = 100,
                LogoHeight = 32,
            },

            // --- Weex ---
            new()
            {
                Id = 32,
                NombreProveedor = "Weex",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#203665"),
                LogoIcon = "logo_weex",
                TextColor = Colors.White,
                LogoWidth = 70,
                LogoHeight = 28,
            },

            // --- Chip Macropay ---
            new()
            {
                Id = 33,
                NombreProveedor = "Chip Macropay",
                Subproducto = null,
                ColorTarjeta = Color.FromArgb("#534789"),
                LogoIcon = "logo_chip_macropay",
                TextColor = Colors.White,
                LogoWidth = 100,
                LogoHeight = 32,
            },

            // --- redi Coppel ---
            new()
            {
                Id = 34,
                NombreProveedor = "redi Coppel",
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