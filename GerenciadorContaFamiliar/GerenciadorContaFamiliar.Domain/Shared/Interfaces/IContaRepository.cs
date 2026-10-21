using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IContaRepository
{
    /// <summary>
    /// Obtém uma conta pelo seu identificador único.
    /// <returns>Conta encontrada ou null se não existir</returns>
    Task<Conta?> ObterPorIdAsync(int id, CancellationToken ct = default);
    /// <summary>
    /// Obtém uma conta pelo seu nome.
    /// <returns>Conta encontrada ou null se não existir</returns>
    Task<Conta?> ObterPorNomeAsync(string nome, CancellationToken ct = default);
    /// <summary>
    /// Obtém a conta principal ativa de um membro.
    /// <returns>Conta principal encontrada ou null se o membro ainda não tiver uma</returns>
    Task<Conta?> ObterContaPrincipalAsync(int membroId, CancellationToken ct = default);
    /// <summary>
    /// Lista todas as contas ativas de um membro.
    /// </summary>
    Task<IReadOnlyList<Conta>> ObterPorMembroAsync(int membroId, CancellationToken ct = default);
    /// <summary>
    /// Adiciona uma nova conta ao repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    Task AdicionarAsync(Conta conta, CancellationToken ct = default);
    /// <summary>
    /// Atualiza uma conta existente no repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AtualizarAsync(Conta conta, CancellationToken ct = default);
    /// <summary>
    /// Remove uma conta pelo seu identificador.
    /// A remoção será persistida no banco de dados quando o UnitOfWork for commitado.
    Task RemoverAsync(int id, CancellationToken ct = default);
}