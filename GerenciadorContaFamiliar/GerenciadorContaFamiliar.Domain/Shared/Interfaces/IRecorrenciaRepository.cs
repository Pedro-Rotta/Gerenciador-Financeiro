using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IRecorrenciaRepository
{
    Task<Recorrencia?> ObterPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Séries ativas das contas que o membro acessa (individuais dele e conjuntas de que
    /// participa), com o que é preciso para gerar as ocorrências: autor, conta (e participantes
    /// da conjunta), categoria e subcategoria.
    /// </summary>
    Task<IReadOnlyList<Recorrencia>> ObterAtivasPorMembroAsync(int membroId, CancellationToken ct = default);

    /// <summary>Séries ativas de uma conta individual.</summary>
    Task<IReadOnlyList<Recorrencia>> ObterAtivasPorContaAsync(int contaId, CancellationToken ct = default);

    /// <summary>Séries ativas de uma conta conjunta; com <paramref name="autorId"/>, só as desse autor.</summary>
    Task<IReadOnlyList<Recorrencia>> ObterAtivasPorContaConjuntaAsync(
        int contaConjuntaId, int? autorId = null, CancellationToken ct = default);

    Task AdicionarAsync(Recorrencia recorrencia, CancellationToken ct = default);

    Task AtualizarAsync(Recorrencia recorrencia, CancellationToken ct = default);
}
