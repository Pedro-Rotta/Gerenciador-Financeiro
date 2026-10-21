using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

public interface IPreferenciaNotificacaoRepository
{
    /// <summary>Preferências salvas do membro; null = nunca salvou (valem os padrões).</summary>
    Task<PreferenciaNotificacao?> ObterPorMembroAsync(int membroId, CancellationToken ct = default);

    /// <summary>Preferências salvas de vários membros (quem nunca salvou não vem).</summary>
    Task<IReadOnlyList<PreferenciaNotificacao>> ObterPorMembrosAsync(
        IEnumerable<int> membroIds, CancellationToken ct = default);

    Task AdicionarAsync(PreferenciaNotificacao preferencia, CancellationToken ct = default);

    Task AtualizarAsync(PreferenciaNotificacao preferencia, CancellationToken ct = default);
}
