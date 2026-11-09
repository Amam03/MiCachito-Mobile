using MiCachito.Mobile.Services.Scanning;
using Xunit;

namespace MiCachito.Mobile.Tests;

/// <summary>
/// Unit tests del parser de codigos de boletos (BilleteParser) — port del
/// parsear_cadena_billete del desktop. Logica PURA: sin Android, sin red.
///
/// Casos de esquina cubiertos (reglas de dominio 2026-09-18 y V4 35/34):
///  - LN 35 digitos: numero[14:19], serie[19:21], sub[21:24]; subcodigo de
///    Zodiaco en 35 = ERROR DE CAPTURA (null).
///  - LN 34 digitos: signo[18:20] + sub[20:23]; subcodigo de edicion con
///    SERIE (Mayor/Superior/Especial/Magno) en 34 = ERROR (null).
///  - Fraccion fuera de 1..20 = null.
///  - TEC: prefijo SE/CL/SS, tipo T|B, longitud 9..12.
///  - Universitarios: 6 digitos.
///  - Rasca (12 dig, prefijo 3) / Prorra (22 dig, posicion [9:12]).
///  - Signo zodiacal invalido en 34 = null.
/// </summary>
public class BilleteParserTests
{
    // ── LN 35 digitos (Mayor/Superior/Especial/Magno: serie numerica) ──────

    [Fact]
    public void Ln35_SeParseaConSerieNumeroYSubcodigo()
    {
        // bloqueEmisor[0:14]=prefijo, [14:19]=numero, [19:21]=serie,
        // [21:24]=sub; luego 3 de fraccion+DV y 8 de fecha.
        // fraccion 050 -> Fraccion 5, DV 0. Fecha 2026-09-24.
        const string codigo = "00000000012345" + "00140" + "2000" + "0000" + "242026";
        //              prefijo(14)   numero  sub  serie? NO: [14:19]="00140",
        // [19:21]="00", [21:24]="000" -> sub 000 no existe -> mejor usar 140.

        // Construir explicito: prefijo 14, numero "12345", serie "07",
        // sub "140" (Mayor), fraccion/vigesimo "050" (5), fecha "24092026" NO:
        // fechaRaw = ultimos 8 = ddMMyyyy.
        const string codigoMayor =
            "01234567890123" +   // prefijo emisor (14)
            "12345" +             // numero de billete [14:19]
            "07" +                // serie [19:21]
            "140" +               // subcodigo Mayor [21:24]
            "050" +               // vigesimo: fraccion 5, DV 0
            "24092026";           // fecha ddMMyyyy

        var p = BilleteParser.ParsearCadena(codigoMayor);

        Assert.NotNull(p);
        Assert.Equal(35, p!.LongitudTotal);
        Assert.True(p.EsLoteriaNacional);
        Assert.False(p.EsZodiaco);
        Assert.Equal("12345", p.NumeroBillete);
        Assert.Equal("07", p.Serie);
        Assert.Equal("140", p.SubCodigo);
        Assert.Equal(5, p.Fraccion);
        Assert.Equal(5, p.Vigesimo);
        Assert.Equal("0", p.DigitoVerificacion);
        Assert.Equal("2026-09-24", p.FechaSorteo);
        Assert.Equal(7000, p.SerieSabana); // 7 * 1000
    }

    [Fact]
    public void Ln35_SubcodigoDeZodiacoEsErrorDeCaptura_Null()
    {
        // Regla 2026-09-18: un 35 con subcodigo de Zodiaco (446) NO es
        // captura valida: Zodiaco es de 34 con signo.
        const string codigo =
            "01234567890123" + "12345" + "07" + "446" + "050" + "24092026";

        Assert.Null(BilleteParser.ParsearCadena(codigo));
    }

    // ── LN 34 digitos (Zodiaco/ZodiacoEsp: signo, sin serie) ───────────────

    [Fact]
    public void Ln34_ZodiacoSeParseaConSigno()
    {
        // bloqueEmisor[14:18]=numero, [18:20]=signo, [20:23]=sub.
        // signo 02 = TAURO, sub 446 = Zodiaco.
        const string codigo =
            "01234567890123" +   // prefijo (14)
            "1234" +              // numero [14:18]
            "02" +                // signo TAURO [18:20]
            "446" +               // sub Zodiaco [20:23]
            "050" +               // fraccion 5, DV 0
            "24092026";           // 34 digitos totales

        var p = BilleteParser.ParsearCadena(codigo);

        Assert.NotNull(p);
        Assert.Equal(34, p!.LongitudTotal);
        Assert.True(p.EsLoteriaNacional);
        Assert.True(p.EsZodiaco); // Serie null en Zodiaco
        Assert.Null(p.Serie);
        Assert.Equal("1234", p.NumeroBillete);
        Assert.Equal("02", p.SignoCodigo);
        Assert.Equal("TAURO", p.SignoNombre);
        Assert.Equal("446", p.SubCodigo);
        Assert.Equal(5, p.Fraccion);
        Assert.Equal(2000, p.SerieSabana); // signo 2 * 1000
    }

    [Fact]
    public void Ln34_SignoInvalidoEsNull()
    {
        // signo 13 no existe (01..12).
        const string codigo =
            "01234567890123" + "1234" + "13" + "446" + "050" + "24092026";

        Assert.Null(BilleteParser.ParsearCadena(codigo));
    }

    [Fact]
    public void Ln34_SubcodigoConSerieEsErrorDeCaptura_Null()
    {
        // Regla 2026-09-18: un 34 con sub de edicion con SERIE (140 Mayor)
        // es error (p. ej. un Mayor al que le faltó un digito).
        const string codigo =
            "01234567890123" + "1234" + "02" + "140" + "050" + "24092026";

        Assert.Null(BilleteParser.ParsearCadena(codigo));
    }

    // ── Fraccion / vigesimo ─────────────────────────────────────────────────

    [Theory]
    [InlineData("00")]   // fraccion 0: invalida
    [InlineData("210")]  // fraccion 21: fuera de rango
    public void Ln35_FraccionFueraDeRangoEsNull(string vigesimo)
    {
        string codigo =
            "01234567890123" + "12345" + "07" + "140" + vigesimo + "24092026";

        Assert.Null(BilleteParser.ParsearCadena(codigo));
    }

    // ── Longitudes / formato ────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]                          // 5 digitos: ningun tipo
    [InlineData("1234567890123456789012345678901234567890")] // 39: ningun tipo
    [InlineData("0123456789012312345071400502409202X")]      // con letra: ningun tipo
    public void CadenasInvalidasDevuelvenNull(string cadena)
    {
        // TEC exige prefijo SE/CL/SS; universitario 6 digitos; rasca 12/22
        // con prefijo de 3 conocido. Ninguna de estas cae en algun tipo.
        Assert.Null(BilleteParser.ParsearCadena(cadena));
    }

    // ── Sorteos TEC ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("SE001T123", "Sorteo Educativo", "001", "123", "T")]
    [InlineData("CL004B456", "Sorteo Dinero de X Vida", "004", "456", "B")]
    [InlineData("SS002T780", "Sorteo Mi Sueño", "002", "780", "T")]
    public void Tec_SeParseaConPrefijoCodigoYBillete(
        string codigo, string nombre, string codigoInterno, string numero, string tipo)
    {
        var p = BilleteParser.ParsearCadena(codigo);

        Assert.NotNull(p);
        Assert.True(p!.EsSorteoTec);
        Assert.False(p.EsLoteriaNacional);
        Assert.Equal(nombre, p.NombreSorteoTec);
        Assert.Equal(codigoInterno, p.CodigoSorteoInterno);
        Assert.Equal(numero, p.NumeroBillete);
        Assert.Equal(tipo, p.TipoCodigoTec);
        Assert.Null(p.Serie);
    }

    [Theory]
    [InlineData("XX001T123")]  // prefijo desconocido
    [InlineData("SE001X123")]  // tipo neither T nor B
    [InlineData("SE001T12A")]  // billete con letra
    [InlineData("SE001T1234567890123456789")] // longitud 25: fuera de 9..12
    public void Tec_InvalidosDevuelvenNull(string codigo)
    {
        Assert.Null(BilleteParser.ParsearCadena(codigo));
    }

    // ── Universitarios ──────────────────────────────────────────────────────

    [Fact]
    public void Universitario_SeisDigitosSeParsea()
    {
        var p = BilleteParser.ParsearCadena("123456");

        Assert.NotNull(p);
        Assert.True(p!.EsSorteoUniversitario);
        Assert.Equal("123456", p.NumeroBillete);
        Assert.False(p.EsLoteriaNacional);
    }

    // ── Rasca / Prorra ──────────────────────────────────────────────────────

    [Fact]
    public void Rasca_DoceDigitosConPrefijoConocidoSeParsea()
    {
        // prefijo 232 = Rasca 10; posicion = ultimos 3.
        var p = BilleteParser.ParsearCadena("232456789123");

        Assert.NotNull(p);
        Assert.True(p!.EsRascaProrra);
        Assert.False(p.EsProrrapido);
        Assert.Equal("Rasca 10", p.NombreProductoRascaProrra);
        Assert.Equal("123", p.NumeroBillete);
    }

    [Fact]
    public void Prorra_VeintidosDigitosPosicionEn912()
    {
        // prefijo 351 = Prorrapido 10; posicion [9:12].
        var p = BilleteParser.ParsearCadena("3514567891234567890123");

        Assert.NotNull(p);
        Assert.True(p!.EsRascaProrra);
        Assert.True(p.EsProrrapido);
        Assert.Equal("Prorrapido 10", p.NombreProductoRascaProrra);
        Assert.Equal("123", p.NumeroBillete); // [9:12]
    }

    [Theory]
    [InlineData("999456789123")]  // prefijo 3 no conocido en 12 digitos
    [InlineData("2324567891234567890123")] // 232 con 22: longitud no casa
    public void RascaProrra_InvalidosDevuelvenNull(string codigo)
    {
        Assert.Null(BilleteParser.ParsearCadena(codigo));
    }
}
