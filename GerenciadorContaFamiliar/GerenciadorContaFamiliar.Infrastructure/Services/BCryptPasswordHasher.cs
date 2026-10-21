using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Infrastructure.Services;

/// <summary>
/// Implementação de hash de senhas usando BCrypt.
/// BCrypt gera automaticamente um salt único para cada hash.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Gera um hash BCrypt da senha em texto plano.
    /// Cada chamada gera um salt diferente (mesma senha = hashes diferentes).
    /// </summary>
    public string Hash(string senha)
    {
        return BCrypt.Net.BCrypt.HashPassword(senha);
    }

    /// <summary>
    /// Verifica se a senha em texto plano corresponde ao hash armazenado.
    /// O BCrypt extrai o salt do hash e reprocessa a senha para comparar.
    /// </summary>
    public bool Verify(string senha, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(senha, hash);
    }
}
