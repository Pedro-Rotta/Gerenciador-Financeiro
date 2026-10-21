using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IMembroRepository
{
    /// <summary>
    /// Obtém um membro pelo seu identificador único.
    /// </summary>
    /// <returns>membro encontrado ou null se não existir</returns>
    Task<Membro?> ObterPorIdAsync(int id, CancellationToken ct = default);
    /// <summary>
    /// Obtém um membro pelo seu nome de usuário (login).
    /// </summary>
    /// <returns>membro encontrado ou null se não existir</returns>
    Task<Membro?> ObterPorNomeDeUsuarioAsync(string nomeDeUsuario, CancellationToken ct = default);
    /// <summary>
    /// Obtém um membro pelo seu email.
    /// </summary>
    /// <returns>membro encontrado ou null se não existir</returns>
    Task<Membro?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    /// <summary>
    /// Obtém um membro pelo seu telefone.
    /// </summary>
    /// <returns>membro encontrado ou null se não existir</returns>
    Task<Membro?> ObterPorTelefoneAsync(string? telefone, CancellationToken ct = default);
    /// <summary>
    /// Adiciona um novo membro ao repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    Task AdicionarAsync(Membro membro, CancellationToken ct = default);
    /// <summary>
    /// Atualiza um membro existente no repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AtualizarAsync(Membro membro, CancellationToken ct = default);
    /// <summary>
    /// Remove um membro pelo seu identificador.
    /// A remoção será persistida no banco de dados quando o UnitOfWork for commitado.
    Task RemoverAsync(int id, CancellationToken ct = default);
}