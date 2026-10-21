using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface INotificacaoRepository
{
    /// <summary>
    /// Obtém uma notificação pelo seu identificador único.
    /// </summary>
    /// <returns>Notificação encontrada ou null se não existir</returns>
    Task<Notificacao?> ObterPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Obtém todas as notificações de um determinado membro.
    /// </summary>
    /// <returns>Lista de notificações do membro informado</returns>
    Task<IReadOnlyList<Notificacao>> ObterPorMembroAsync(int membroId, CancellationToken ct = default);

    /// <summary>
    /// Adiciona uma nova notificação ao repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    /// <summary>Chaves dos avisos do membro que começam com o prefixo (ex.: "vencimento:").</summary>
    Task<HashSet<string>> ObterChavesAsync(int membroId, string prefixo, CancellationToken ct = default);

    Task AdicionarAsync(Notificacao notificacao, CancellationToken ct = default);

    /// <summary>
    /// Atualiza uma notificação existente no repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AtualizarAsync(Notificacao notificacao, CancellationToken ct = default);

    /// <summary>
    /// Remove uma notificação pelo seu identificador.
    /// A remoção será persistida no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task RemoverAsync(int id, CancellationToken ct = default);
}
