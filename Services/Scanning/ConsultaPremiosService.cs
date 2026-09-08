namespace MiCachito.Mobile.Services.Scanning;

/// <summary>
/// Resultado de la consulta de premios para un boleto escaneado.
/// </summary>
public enum ResultadoPremio
{
    TienePremio,
    NoTienePremio,
    Reintegro,
}

/// <summary>
/// Consulta de premios — SERVICIO MOCK (fase solo-interfaz, sin backend).
///
/// Catalogo de demostracion con los 3 boletos REALES decodificados de los
/// PDFs de docs/UI (mayor/superior/zodiaco, sep-2026). Cada uno con el
/// resultado asignado que el usuario confirmo para la demo:
///   Mayor 03787 serie 02 fr 15 (21-abr-2026)  -> TIENE PREMIO
///   Superior 56592 serie 01 fr 14 (24-abr-2026) -> NO TIENE PREMIO
///   Zodiaco 0749 signo 09 SAGITARIO fr 09 (05-jul-2026) -> REINTEGRO
///
/// Reglas del flujo (acordadas con el usuario):
///   - Codigo valido no catalogado -> NoTienePremio.
///   - Codigo invalido (no parsea) -> la pantalla de escaneo muestra aviso
///     sin navegar al resultado.
/// En la fase de integracion este servicio se sustituye por la llamada al
/// endpoint del backend (la consulta del desktop manda billete, serie/signo,
/// fraccion, fecha y subcodigo).
/// </summary>
public sealed class ConsultaPremiosService
{
    private sealed record CatalogoEntry(
        string CodigoCompleto,
        ResultadoPremio Resultado,
        string Detalle);

    private readonly List<CatalogoEntry> _catalogo = new()
    {
        // Mayor 4010, 21-abr-2026 — QR real del PDF (fraccion 15, digito verificador 3)
        new("00064284003059037870212615321042026", ResultadoPremio.TienePremio,
            "Sorteo Mayor 4010 - billete 03787 serie 02, fraccion 15"),
        // Superior 2881, 24-abr-2026 — QR real del PDF (fraccion 14)
        new("00063586003059565920124914924042026", ResultadoPremio.NoTienePremio,
            "Sorteo Superior 2881 - billete 56592 serie 01, fraccion 14"),
        // Zodiaco 1751, 05-jul-2026 — QR real del PDF (signo 09 SAGITARIO, fraccion 09)
        new("0005314000305907490944109305072026", ResultadoPremio.Reintegro,
            "Sorteo Zodiaco 1751 - billete 0749 signo SAGITARIO, fraccion 09"),
    };

    /// <summary>
    /// Consulta el resultado de un boleto ya parseado. Mock: match exacto
    /// por codigo completo contra el catalogo; si no esta, NoTienePremio.
    /// </summary>
    public (ResultadoPremio Resultado, string Detalle) Consultar(BilleteParseado parseado)
    {
        foreach (CatalogoEntry e in _catalogo)
        {
            if (e.CodigoCompleto == parseado.CodigoCompleto)
            {
                return (e.Resultado, e.Detalle);
            }
        }

        return (ResultadoPremio.NoTienePremio, "Boleto no premiado");
    }
}
