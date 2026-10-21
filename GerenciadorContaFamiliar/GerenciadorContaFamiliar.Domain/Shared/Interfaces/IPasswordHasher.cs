namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

/// <summary>
/// Abstração de hash de senhas. A implementação concreta fica na Infrastructure.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Gera um hash a partir da senha em texto plano.
    /// </summary>
    string Hash(string senha);

    /// <summary>
    /// Verifica se a senha em texto plano corresponde ao hash armazenado.
    /// </summary>
    bool Verify(string senha, string hash);
}
