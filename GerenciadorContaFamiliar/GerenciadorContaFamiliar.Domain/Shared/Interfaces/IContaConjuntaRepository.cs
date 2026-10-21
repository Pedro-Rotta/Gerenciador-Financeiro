using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IContaConjuntaRepository
{
    /// <summary>
    /// Obtém uma conta conjunta pelo seu identificador único.
    /// <returns>Conta conjunta encontrada ou null se não existir</returns>
    Task<ContaConjunta?> ObterPorIdAsync(int id, CancellationToken ct = default);
    /// <summary>
    /// Obtém todos os membros participantes de uma conta conjunta.
    /// <returns>Lista de membros da conta conjunta</returns>
    Task<IReadOnlyList<Membro>> ObterMembrosDaContaConjuntaAsync(int contaId, CancellationToken ct = default);
    /// <summary>
    /// Lista as contas conjuntas ativas das quais o membro participa (como criador ou participante).
    /// </summary>
    Task<IReadOnlyList<ContaConjunta>> ObterPorMembroAsync(int membroId, CancellationToken ct = default);
    /// <summary>
    /// Adiciona uma nova conta conjunta ao repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    Task AdicionarAsync(ContaConjunta conta, CancellationToken ct = default);
    /// <summary>
    /// Atualiza uma conta conjunta existente no repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AtualizarAsync(ContaConjunta conta, CancellationToken ct = default);
    /// <summary>
    /// Remove uma conta conjunta pelo seu identificador.
    /// A remoção será persistida no banco de dados quando o UnitOfWork for commitado.
    Task RemoverAsync(int id, CancellationToken ct = default);
}