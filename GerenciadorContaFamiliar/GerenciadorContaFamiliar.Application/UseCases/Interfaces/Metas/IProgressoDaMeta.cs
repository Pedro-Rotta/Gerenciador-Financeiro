using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;

/// <summary>
/// Recalcula o valor atual de uma meta: soma das transações vinculadas ativas e CONCLUÍDAS
/// (pendente ainda não foi guardada). Não persiste: quem chama faz o SaveChanges.
/// </summary>
public interface IProgressoDaMeta
{
    /// <param name="incluir">Transação sendo vinculada agora (a vinculação ainda não foi gravada).</param>
    /// <param name="ignorarTransacaoId">Transação sendo desvinculada agora.</param>
    /// <param name="avisarDono">Se informado, grava o aviso de progresso (marcos e conclusão) para ele,
    /// quando a preferência "Progresso da meta" estiver ligada.</param>
    Task RecalcularAsync(
        Meta meta,
        Transacao? incluir = null,
        int? ignorarTransacaoId = null,
        Membro? avisarDono = null,
        CancellationToken ct = default);

    /// <summary>Recalcula a meta à qual a transação está vinculada, se houver.</summary>
    Task RecalcularDaTransacaoAsync(Transacao transacao, bool avisar, CancellationToken ct = default);
}
