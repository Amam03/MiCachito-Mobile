namespace MiCachito.Mobile.Models.Responses;

/// <summary>
/// Expendio autenticado (subobjeto de login/verify mobile).
/// Campo "usuario" = credencial del expendio (billeteros_expendios.usuario).
/// </summary>
public class ExpendioInfo
{
    public int IdExpendio { get; set; }

    public string? Usuario { get; set; }
}
