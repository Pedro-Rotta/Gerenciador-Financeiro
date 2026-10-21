using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Recorrencias;

public class GeradorDeOcorrencias : IGeradorDeOcorrencias
{
    /// <summary>Antecedência da geração: a maior do lembrete de vencimento.</summary>
    public const int DiasDeAntecedencia = 7;

    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IAvisosDeLancamento _avisosDeLancamento;

    public GeradorDeOcorrencias(
        ITransacaoRepository transacaoRepository,
        IAvisosDeLancamento avisosDeLancamento)
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _avisosDeLancamento = avisosDeLancamento ?? throw new ArgumentNullException(nameof(avisosDeLancamento));
    }

    public async Task GerarAsync(Recorrencia serie, CancellationToken ct = default)
    {
        if (serie is null)
            throw new ArgumentNullException(nameof(serie));

        // Se a série ficou dias sem gerar (ou começou no passado), gera de uma vez o que faltou
        var ate = Relogio.HojeBrasilia().AddDays(DiasDeAntecedencia);
        while (serie.Ativa && serie.ProximaData <= ate)
        {
            // Categoria/subcategoria excluídas chegam nulas: a ocorrência sai sem elas
            var ocorrencia = new Transacao(
                serie.Valor,
                serie.Descricao,
                serie.ProximaData,
                serie.Tipo,
                serie.Classificacao,
                recorrente: true,
                serie.Periodicidade,
                serie.Membro,
                serie.Categoria,
                serie.SubCategoria,
                serie.Conta,
                serie.ContaConjunta,
                serie.StatusDaOcorrencia(serie.ProximaData));
            ocorrencia.VincularARecorrencia(serie, serie.RegistrarOcorrencia());

            var saldoAnterior = serie.Conta?.Saldo ?? serie.ContaConjunta!.Saldo;
            ocorrencia.AplicarEmConta();
            await _transacaoRepository.AdicionarAsync(ocorrencia, ct);
            await _avisosDeLancamento.AvisarAsync(ocorrencia, saldoAnterior, geradaPelaSerie: true, ct);
        }
    }
}
