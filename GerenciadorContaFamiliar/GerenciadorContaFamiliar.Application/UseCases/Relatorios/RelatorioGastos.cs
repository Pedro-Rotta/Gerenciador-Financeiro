using GerenciadorContaFamiliar.Application.DTOs.Requests.Relatorios;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;

namespace GerenciadorContaFamiliar.Application.UseCases.Relatorios;

/// <summary>
/// Relatório completo de gastos.
/// - Filtro por SubCategoria ("Gastos com Combustível") → agrupa POR CATEGORIA
///   (Carro R$300 | MotoSerra R$30).
/// - Filtro por Categoria ("Gastos com Carro") → agrupa POR SUBCATEGORIA
///   (Combustível R$300 | Peças R$1500 | Sem subcategoria R$X).
/// - ApenasSemSubcategoria = true → apenas "Carro sem categoria".
/// </summary>
public class RelatorioGastosUseCase : IRelatorioGastosUseCase
{
    private readonly ITransacaoRepository _transacaoRepository;

    public RelatorioGastosUseCase(ITransacaoRepository transacaoRepository)
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
    }

    public async Task<RelatorioGastosResponse> ExecutarAsync(
        RelatorioGastosRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (request.IdCategoria.HasValue && request.IdSubCategoria.HasValue)
            throw new ArgumentException("Informe apenas um filtro por vez: IdCategoria OU IdSubCategoria.");

        // 1. Buscar transações filtradas (gastos = SAIDA), com Categoria e SubCategoria carregadas
        var transacoes = (await _transacaoRepository.ObterParaRelatorioAsync(
            request.MembroId,
            request.IdCategoria,
            request.IdSubCategoria,
            request.ApenasSemSubcategoria,
            request.DataInicio,
            request.DataFim,
            cancellationToken))
            // Só as pagas, como o saldo: pendentes e atrasadas entram quando forem pagas
            .Where(t => t.Tipo == Transacao.TipoTransacao.SAIDA && t.Status == Transacao.StatusTransacao.CONCLUIDA)
            .ToList();

        // 2. Agrupar conforme o filtro
        var itens = new List<RelatorioGastosItem>();

        if (request.IdSubCategoria.HasValue)
        {
            // Filtrou por subcategoria → agrupa POR CATEGORIA
            itens = transacoes
                .GroupBy(t => t.Categoria?.Nome ?? "Sem categoria")
                .OrderByDescending(g => g.Sum(t => t.Valor))
                .Select(g => new RelatorioGastosItem
                {
                    Nome = g.Key,
                    Valor = g.Sum(t => t.Valor),
                    Quantidade = g.Count(),
                })
                .ToList();
        }
        else
        {
            // Filtrou por categoria (ou nada) → agrupa POR SUBCATEGORIA
            itens = transacoes
                .GroupBy(t => t.SubCategoria?.Nome ?? "Sem subcategoria")
                .OrderByDescending(g => g.Sum(t => t.Valor))
                .Select(g => new RelatorioGastosItem
                {
                    Nome = g.Key,
                    Valor = g.Sum(t => t.Valor),
                    Quantidade = g.Count(),
                })
                .ToList();
        }

        // 3. Montar resposta
        return new RelatorioGastosResponse
        {
            ValorTotal = transacoes.Sum(t => t.Valor),
            QuantidadeTotal = transacoes.Count,
            Itens = itens,
        };
    }
}
