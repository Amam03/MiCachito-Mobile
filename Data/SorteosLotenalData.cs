using MiCachito.Mobile.Models.Entities;

namespace MiCachito.Mobile.Data;

/// <summary>
/// Catálogo estático de los 8 sorteos de Lotería Nacional (LOTENAL).
/// Colores extraídos del mockup docs/UI/Vender/6_SrB_LOTENAL.png.
/// </summary>
public static class SorteosLotenalData
{
    public static IReadOnlyList<SorteoLotenal> Sorteos { get; } =
    [
        new()
        {
            Id = 1,
            Nombre = "SORTEO MAYOR",
            Linea1 = "SORTEO",
            Linea2 = "MAYOR",
            Precio = 30m,
            ColorHex = "#FFB600",
            SubcodigoSorteo = "140"
        },
        new()
        {
            Id = 2,
            Nombre = "SORTEO SUPERIOR",
            Linea1 = "SORTEO",
            Linea2 = "SUPERIOR",
            Precio = 40m,
            ColorHex = "#4D9D2E",
            SubcodigoSorteo = "261"
        },
        new()
        {
            Id = 3,
            Nombre = "SORTEO ZODIACO",
            Linea1 = "SORTEO",
            Linea2 = "ZODIACO",
            Precio = 20m,
            ColorHex = "#ED40A9",
            SubcodigoSorteo = "446"
        },
        new()
        {
            Id = 4,
            Nombre = "SORTEO ZODIACO ESPECIAL",
            Linea1 = "SORTEO ZODIACO",
            Linea2 = "ESPECIAL",
            Precio = 35m,
            ColorHex = "#C96D08",
            SubcodigoSorteo = "448"
        },
        new()
        {
            Id = 5,
            Nombre = "SORTEO ESPECIAL",
            Linea1 = "SORTEO",
            Linea2 = "ESPECIAL",
            Precio = 60m,
            ColorHex = "#00A0DE",
            SubcodigoSorteo = "516"
        },
        new()
        {
            Id = 6,
            Nombre = "SORTEO GRAN ESPECIAL",
            Linea1 = "SORTEO GRAN",
            Linea2 = "ESPECIAL",
            Precio = 250m,
            ColorHex = "#1565C0"
        },
        new()
        {
            Id = 7,
            Nombre = "SORTEO MAGNO",
            Linea1 = "SORTEO",
            Linea2 = "MAGNO",
            Precio = 120m,
            ColorHex = "#C62828"
        },
        new()
        {
            Id = 8,
            Nombre = "SORTEO GORDITO NAVIDEÑO",
            Linea1 = "SORTEO GORDITO",
            Linea2 = "NAVIDEÑO",
            Precio = 120m,
            ColorHex = "#6A1B9A"
        },
    ];
}
