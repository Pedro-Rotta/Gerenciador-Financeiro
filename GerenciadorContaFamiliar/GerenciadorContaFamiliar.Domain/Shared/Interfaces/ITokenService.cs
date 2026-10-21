namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

/// <summary>
/// Abstração de geração de tokens de autenticação.
/// A implementação concreta (JWT) fica na Infrastructure.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Gera um token de autenticação para o membro informado.
    /// </summary>
    /// <param name="membroId">ID do membro</param>
    /// <param name="email">Email do membro</param>
    /// <param name="versaoSessao">Versão da sessão do membro (troca de senha derruba as anteriores)</param>
    /// <returns>Token string + data de expiração</returns>
    (string Token, DateTime ExpiraEm) GerarToken(int membroId, string email, int versaoSessao);
}
