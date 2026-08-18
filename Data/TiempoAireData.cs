using System.Collections.ObjectModel;
using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

/// <summary>
/// Fuente central de datos estáticos para Tiempo Aire (UI-only).
/// Contiene los 34 proveedores con sus montos disponibles.
/// Cuando exista el endpoint del backend, se reemplazará por una llamada API.
/// </summary>
public static class TiempoAireData
{
    /// <summary>
    /// Lista inmutable de los 34 proveedores con sus montos.
    /// </summary>
    public static IReadOnlyList<ProveedorTiempoAire> Proveedores { get; } = BuildProveedores();

    /// <summary>
    /// Diccionario Id → Proveedor para búsquedas rápidas desde navegación.
    /// </summary>
    public static IReadOnlyDictionary<int, ProveedorTiempoAire> PorId { get; } =
        Proveedores.ToDictionary(p => p.Id);

    /// <summary>
    /// Obtiene un proveedor por Id. Retorna null si no existe.
    /// </summary>
    public static ProveedorTiempoAire? ObtenerPorId(int id) =>
        PorId.TryGetValue(id, out var p) ? p : null;

    // ================================================================
    //  Construcción de la lista de proveedores
    // ================================================================
    private static IReadOnlyList<ProveedorTiempoAire> BuildProveedores()
    {
        var lista = new List<ProveedorTiempoAire>
        {
            // ===================================================
            // --- Imagen 3.1 (1-10): Telcel ×4, AT&T, Movistar, Bait ×4
            // ===================================================

            // 1 — Telcel RECARGA
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
                Montos = [10m, 20m, 30m, 50m, 80m, 100m, 150m, 200m, 300m, 500m],
            },
            // 2 — Telcel PAQUETE
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
                Montos = [10m, 20m, 30m, 50m, 80m, 100m, 150m, 200m, 270m, 300m, 400m, 500m, 1200m, 2400m],
            },
            // 3 — Telcel INTERNET
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
                Montos = [10m, 20m, 30m, 50m, 80m, 100m, 150m, 200m, 300m, 500m],
            },
            // 4 — Telcel X TIEMPO
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
                Montos = [10m, 20m, 25m],
            },
            // 5 — AT&T
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
                Montos = [10m, 15m, 20m, 30m, 50m, 70m, 100m, 120m, 150m, 200m, 300m, 500m, 1000m],
            },
            // 6 — Movistar
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
                Montos = [10m, 20m, 30m, 0m, 50m, 60m, 70m, 80m, 100m, 120m, 140m, 150m, 200m, 250m, 300m, 500m],
            },
            // 7 — Bait
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
                Montos = [30m, 50m, 60m, 100m, 120m, 125m, 200m, 230m, 300m],
            },
            // 8 — Bait Internet
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
                Montos = [110m, 210m, 410m],
            },
            // 9 — Bait Internet en Casa
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
                Montos = [99m, 349m],
            },
            // 10 — Bait Paquetes
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
                Montos = [550m, 800m, 1050m, 1500m, 2000m, 2900m],
            },

            // ===================================================
            // --- Imagen 3.2 (11-20): Internet Bienestar, CFE, Virquin,
            //     Soriana ×2, DIRY, OUI, PILLO FON, Cierto, ComparT fon
            // ===================================================

            // 11 — Internet para el Bienestar
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
                Montos = [50m, 70m, 100m, 130m, 150m, 190m, 250m, 300m, 500m],
            },
            // 12 — CFE Internet
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
                Montos = [35m, 85m, 105m, 155m, 160m, 180m, 220m, 275m, 325m, 425m, 510m, 520m, 755m],
            },
            // 13 — Virquin Mobile
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
                Montos = [20m, 30m, 40m, 50m, 100m, 150m, 200m, 300m, 500m],
            },
            // 14 — Soriana Movil
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
                Montos = [30m, 50m, 100m, 150m, 200m, 500m],
            },
            // 15 — Soriana Movil Paquetes
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
                Montos = [30m, 50m, 100m, 130m, 150m, 250m],
            },
            // 16 — DIRY Móvil
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
                Montos = [80m, 150m, 200m, 300m, 400m, 500m],
            },
            // 17 — OUI
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
                Montos = [10m, 15m, 20m, 25m, 30m, 35m, 40m, 45m, 50m, 60m, 80m, 100m, 120m, 150m, 200m, 240m, 300m, 350m],
            },
            // 18 — PILLO FON
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
                Montos = [90m, 200m, 350m, 420m, 600m],
            },
            // 19 — Cierto
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
                Montos = [20m, 30m, 50m, 100m, 200m, 300m, 500m],
            },
            // 20 — ComparT fon
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
                Montos = [10m, 20m, 30m, 40m, 50m, 70m, 100m, 150m, 200m],
            },

            // ===================================================
            // --- Imagen 3.3 (21-30): Flash MOBILE, FreedomPop, Mi Movil,
            //     netwey, Yobi, rin cel, Ultracel, VALOR TELECOM ×3
            // ===================================================

            // 21 — Flash MOBILE
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
                Montos = [10m, 20m, 30m, 40m, 50m, 60m, 70m, 80m, 100m, 120m, 150m, 200m, 250m, 300m, 500m],
            },
            // 22 — FreedomPop
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
                Montos = [30m, 50m, 80m, 100m, 150m, 200m],
            },
            // 23 — Mi Movil
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
                Montos = [50m, 65m, 80m, 100m, 150m, 165m, 200m, 280m, 300m, 400m, 750m],
            },
            // 24 — netwey
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
                Montos = [40m, 65m, 125m, 250m],
            },
            // 25 — Yobi
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
                Montos = [30m, 50m, 100m, 150m, 200m, 500m],
            },
            // 26 — rin cel
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
                Montos = [40m, 50m, 80m, 120m, 135m, 150m, 170m, 220m, 250m, 299m, 320m],
            },
            // 27 — Ultracel
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
                Montos = [55m, 80m, 105m, 110m],
            },
            // 28 — VALOR TELECOM
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
                Montos = [100m, 150m, 239m, 360m, 450m, 590m],
            },
            // 29 — VALOR TELECOM CASA
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
                Montos = [99m, 110m, 349m, 399m, 439m],
            },
            // 30 — VALOR TELECOM PAQUETES
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
                Montos = [100m, 239m, 360m, 450m, 590m],
            },

            // ===================================================
            // --- Imagen 3.4 (31-34): Wimo telecom, Weex,
            //     Chip Macropay, redi Coppel
            // ===================================================

            // 31 — Wimo telecom
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
                Montos = [35m, 65m, 125m, 130m, 190m, 250m, 375m, 625m],
            },
            // 32 — Weex
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
                Montos = [10m, 20m, 30m, 40m, 50m, 60m, 70m, 80m, 100m, 120m, 150m, 200m, 250m, 300m, 400m, 500m],
            },
            // 33 — Chip Macropay
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
                Montos = [50m, 70m, 100m, 130m, 200m, 250m, 500m],
            },
            // 34 — redi Coppel
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
                Montos = [60m, 80m, 100m, 130m, 150m, 200m, 280m, 350m],
            },
        };

        return new ReadOnlyCollection<ProveedorTiempoAire>(lista);
    }
}
