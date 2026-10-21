using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

/// <summary>
/// Interface do repositório para a tabela de junção MetaTransacao.
/// Define operações de acesso a dados para vincular/desvincular transações de metas.
/// </summary>
public interface IMetaTransacaoRepository
{
    /// <summary>
    /// Obtém uma vinculação pelo seu identificador único.
    /// </summary>
    Task<MetaTransacao?> ObterPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Obtém todas as vinculações de uma meta específica.
    /// Inclui a navegação da Transacao para obter os valores.
    /// </summary>
    Task<IReadOnlyList<MetaTransacao>> ObterPorMetaAsync(int metaId, CancellationToken ct = default);

    /// <summary>
    /// Verifica se uma transação já está vinculada a alguma meta.
    /// </summary>
    Task<bool> TransacaoJaVinculadaAsync(int transacaoId, CancellationToken ct = default);

    /// <summary>
    /// Obtém a vinculação de uma transação (cada transação fica em no máximo uma meta), ou null.
    /// </summary>
    Task<MetaTransacao?> ObterPorTransacaoAsync(int transacaoId, CancellationToken ct = default);

    /// <summary>
    /// Obtém a vinculação entre uma meta e uma transação específica.
    /// </summary>
    Task<MetaTransacao?> ObterPorMetaETransacaoAsync(int metaId, int transacaoId, CancellationToken ct = default);

    /// <summary>
    /// Adiciona uma nova vinculação ao repositório.
    /// A vinculação será persistida no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AdicionarAsync(MetaTransacao metaTransacao, CancellationToken ct = default);

    /// <summary>
    /// Remove uma vinculação pelo seu identificador.
    /// A remoção será persistida no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task RemoverAsync(int id, CancellationToken ct = default);
}
