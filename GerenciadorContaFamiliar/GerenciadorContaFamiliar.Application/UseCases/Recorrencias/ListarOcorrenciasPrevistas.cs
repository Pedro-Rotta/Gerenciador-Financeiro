using GerenciadorContaFamiliar.Application.DTOs.Requests.Recorrencias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Recorrencias;

/// <summary>
/// Prévia das ocorrências das séries MENSAIS (com parcelas ou sem fim) até o fim do próximo mês
/// que ainda não foram lançadas — elas são lançadas de verdade 7 dias antes do vencimento.
/// Só leitura: não entra no saldo.
/// </summary>
public class ListarOcorrenciasPrevistasUseCase : IListarOcorrenciasPrevistasUseCase
{
    private readonly IRecorrenciaRepository _recorrenciaRepository;

    public ListarOcorrenciasPrevistasUseCase(IRecorrenciaRepository recorrenciaRepository)
    {
        _recorrenciaRepository = recorrenciaRepository ?? throw new ArgumentNullException(nameof(recorrenciaRepository));
    }

    public async Task<IEnumerable<OcorrenciaPrevistaResponse>> ExecutarAsync(
        ListarOcorrenciasPrevistasRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var ate = FimDoProximoMes();
        var series = await _recorrenciaRepository.ObterAtivasPorMembroAsync(request.MembroId, ct);

        return series
            .Where(s => s.Periodicidade == Transacao.PeriodicidadeTransacao.MENSAL &&
                        (s.Conta is not null || s.ContaConjunta is not null))
            .SelectMany(s => s.ProximasOcorrencias(ate).Select(o => new OcorrenciaPrevistaResponse
            {
                RecorrenciaId = s.Id,
                DataPrevista = o.Data,
                NumeroOcorrencia = o.Numero,
                TotalOcorrencias = s.TotalOcorrencias,
                Valor = s.Valor,
                Descricao = s.Descricao,
                Tipo = s.Tipo,
                MembroId = s.Membro.Id,
                CategoriaId = s.Categoria?.Id,
                ContaId = s.Conta?.Id,
                ContaConjuntaId = s.ContaConjunta?.Id,
            }))
            .OrderBy(o => o.DataPrevista)
            .ToList();
    }

    /// <summary>Último dia do mês seguinte ao atual (Brasília): o mês atual e o próximo.</summary>
    internal static DateOnly FimDoProximoMes()
    {
        var hoje = Relogio.HojeBrasilia();
        return new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(2).AddDays(-1);
    }
}
