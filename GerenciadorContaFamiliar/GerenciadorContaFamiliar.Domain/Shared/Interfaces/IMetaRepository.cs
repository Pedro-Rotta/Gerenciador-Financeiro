using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IMetaRepository
{
    /// <summary>
    /// Obtém uma meta pelo seu identificador único.
    /// </summary>
    /// <returns>Meta encontrada ou null se não existir</returns>
    Task<Meta?> ObterPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Obtém todas as metas de um determinado membro.
    /// Inclui as transações vinculadas (MetaTransacoes).
    /// </summary>
    /// <returns>Lista de metas do membro informado</returns>
    Task<IReadOnlyList<Meta>> ObterPorMembroAsync(int membroId, CancellationToken ct = default);

    /// <summary>
    /// Obtém uma meta com todas as transações vinculadas carregadas.
    /// </summary>
    Task<Meta?> ObterComTransacoesAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Adiciona uma nova meta ao repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AdicionarAsync(Meta meta, CancellationToken ct = default);

    /// <summary>
    /// Atualiza uma meta existente no repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AtualizarAsync(Meta meta, CancellationToken ct = default);

    /// <summary>
    /// Remove uma meta pelo seu identificador.
    /// A remoção será persistida no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task RemoverAsync(int id, CancellationToken ct = default);
}
