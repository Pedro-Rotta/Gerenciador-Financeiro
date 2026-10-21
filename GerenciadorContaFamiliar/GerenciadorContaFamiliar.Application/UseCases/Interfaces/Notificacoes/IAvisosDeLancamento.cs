using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;

/// <summary>
/// Avisos de "transação lançada", num único ponto: lançamento manual e ocorrência gerada por
/// série recorrente. As notificações são persistidas no SaveChanges de quem chamou.
/// </summary>
public interface IAvisosDeLancamento
{
    /// <param name="transacao">Transação já aplicada na conta (saldo atualizado) e adicionada ao repositório.</param>
    /// <param name="saldoAnterior">Saldo da conta antes do lançamento.</param>
    /// <param name="geradaPelaSerie">Ocorrência gerada automaticamente: só os avisos de saldo e de gasto.</param>
    Task AvisarAsync(
        Transacao transacao,
        decimal saldoAnterior,
        bool geradaPelaSerie,
        CancellationToken ct = default);

    /// <summary>
    /// Só os avisos de saldo negativo e saldo baixo (ex.: "Conta paga", quando o saldo muda
    /// na troca de status).
    /// </summary>
    /// <param name="saldoAnterior">Saldo da conta antes da mudança.</param>
    Task AvisarSaldoAsync(Transacao transacao, decimal saldoAnterior, CancellationToken ct = default);
}
