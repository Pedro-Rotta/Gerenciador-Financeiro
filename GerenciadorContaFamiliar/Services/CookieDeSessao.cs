namespace GerenciadorContaFamiliar.Services;

/// <summary>
/// Cookie que guarda o JWT da sessão. HttpOnly: o JavaScript do front não consegue lê-lo,
/// então uma falha de XSS não rouba a sessão. SameSite=Strict: não vai em requisições de outros sites.
/// </summary>
public static class CookieDeSessao
{
    public const string Nome = "sessao";

    public static CookieOptions Opcoes(DateTime expiraEm) => new()
    {
        HttpOnly = true,
        Secure = true,              // localhost conta como seguro mesmo em http
        SameSite = SameSiteMode.Strict,
        Path = "/api",
        Expires = expiraEm
    };
}
