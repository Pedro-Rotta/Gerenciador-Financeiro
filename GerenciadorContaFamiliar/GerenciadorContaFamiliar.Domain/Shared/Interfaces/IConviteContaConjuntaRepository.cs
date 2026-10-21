using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IConviteContaConjuntaRepository
{
    /// <summary>
    /// Obtém um convite pelo seu identificador único, com a conta conjunta,
    /// o convidado e quem convidou carregados.
    /// </summary>
    /// <returns>Convite encontrado ou null se não existir</returns>
    Task<ConviteContaConjunta?> ObterPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Obtém os convites PENDENTES recebidos por um membro (mais recentes primeiro).
    /// </summary>
    Task<IReadOnlyList<ConviteContaConjunta>> ObterPendentesDoConvidadoAsync(int convidadoId, CancellationToken ct = default);

    /// <summary>
    /// Obtém os convites PENDENTES de uma conta conjunta (mais recentes primeiro).
    /// </summary>
    Task<IReadOnlyList<ConviteContaConjunta>> ObterPendentesDaContaAsync(int contaConjuntaId, CancellationToken ct = default);

    /// <summary>
    /// Verifica se já existe um convite PENDENTE para o membro nesta conta conjunta.
    /// </summary>
    Task<bool> ExistePendenteAsync(int contaConjuntaId, int convidadoId, CancellationToken ct = default);

    /// <summary>
    /// Adiciona um novo convite ao repositório.
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AdicionarAsync(ConviteContaConjunta convite, CancellationToken ct = default);

    /// <summary>
    /// Atualiza um convite existente no repositório (aceite, recusa ou cancelamento).
    /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
    /// </summary>
    Task AtualizarAsync(ConviteContaConjunta convite, CancellationToken ct = default);
}
