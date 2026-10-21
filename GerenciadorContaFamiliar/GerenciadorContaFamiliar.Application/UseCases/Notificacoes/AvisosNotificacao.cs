using System.Globalization;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

/// <summary>
/// Apoio aos avisos gerados pelos casos de uso: envio para os participantes de uma conta conjunta
/// e textos padronizados. As notificações são persistidas no SaveChanges do próprio caso de uso.
/// </summary>
public static class AvisosNotificacao
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Avisa todos os participantes da conta conjunta, exceto quem fez a ação.</summary>
    public static async Task AvisarOutrosParticipantesAsync(
        INotificacaoRepository notificacaoRepository,
        ContaConjunta contaConjunta,
        int autorId,
        string mensagem,
        Notificacao.GravidadeNotificacao gravidade,
        CancellationToken ct = default)
    {
        foreach (var participante in contaConjunta.Membros.Where(m => m.Id != autorId))
        {
            var notificacao = new Notificacao(
                mensagem, participante, Notificacao.TipoNotificacao.CONTA_CONJUNTA, gravidade);
            await notificacaoRepository.AdicionarAsync(notificacao, ct);
        }
    }

    /// <summary>
    /// Avisa os outros participantes sobre um lançamento (lançou, editou, excluiu...): só quem
    /// ligou "Lançamentos de outros participantes" nas preferências.
    /// </summary>
    public static async Task AvisarOutrosParticipantesSobreLancamentoAsync(
        INotificacaoRepository notificacaoRepository,
        IPreferenciaNotificacaoRepository preferenciaRepository,
        ContaConjunta contaConjunta,
        int autorId,
        string mensagem,
        Notificacao.GravidadeNotificacao gravidade,
        CancellationToken ct = default)
    {
        var outros = contaConjunta.Membros.Where(m => m.Id != autorId).ToList();
        var preferencias = await PreferenciasDosMembrosAsync(preferenciaRepository, outros.Select(m => m.Id), ct);
        foreach (var participante in outros.Where(m => preferencias[m.Id].LancamentosOutros))
            await notificacaoRepository.AdicionarAsync(new Notificacao(
                mensagem, participante, Notificacao.TipoNotificacao.CONTA_CONJUNTA, gravidade), ct);
    }

    /// <summary>Preferências de cada membro; quem nunca salvou recebe os padrões.</summary>
    public static async Task<Dictionary<int, PreferenciaNotificacao>> PreferenciasDosMembrosAsync(
        IPreferenciaNotificacaoRepository preferenciaRepository,
        IEnumerable<int> membroIds,
        CancellationToken ct = default)
    {
        var ids = membroIds.Distinct().ToList();
        var salvas = (await preferenciaRepository.ObterPorMembrosAsync(ids, ct)).ToDictionary(p => p.MembroId);
        return ids.ToDictionary(id => id, id => salvas.GetValueOrDefault(id) ?? new PreferenciaNotificacao(id));
    }

    /// <summary>Ex.: "saída \"Mercado\" de R$ 120,50" ou "entrada de R$ 3.000,00" (sem artigo).</summary>
    public static string DescreverLancamento(Transacao transacao)
    {
        var tipo = transacao.EhEntrada() ? "entrada" : "saída";
        var descricao = string.IsNullOrWhiteSpace(transacao.Descricao) ? "" : $" \"{transacao.Descricao}\"";
        return $"{tipo}{descricao} de {Moeda(transacao.Valor)}";
    }

    /// <summary>Ex.: "R$ 1.234,56" ou "-R$ 80,00".</summary>
    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);
}
