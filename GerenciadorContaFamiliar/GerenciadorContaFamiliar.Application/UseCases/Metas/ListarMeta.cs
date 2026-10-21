using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Metas;

public class ListarMetaUseCase : IListarMetaUseCase
{
    private readonly IMetaRepository _metaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;

    public ListarMetaUseCase(
        IMetaRepository metaRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository)
    {
        _metaRepository = metaRepository;
        _contaRepository = contaRepository;
        _contaConjuntaRepository = contaConjuntaRepository;
    }

    public async Task<IEnumerable<MetaResponse>> ExecutarAsync(
        ListarMetaRequest request, 
        CancellationToken ct = default)
    {
        // 1. Validação básica do request
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // Busca metas vinculadas a este Membro
        var metas = await _metaRepository.ObterPorMembroAsync(request.MembroId, ct);
        return metas.Select(c => new MetaResponse
        {
            Id = c.Id,
            Nome = c.Nome,
            ValorAlvo = c.ValorAlvo,
            ValorAtual = c.ValorAtual,
            Prazo = c.Prazo,
            Concluida = c.Concluida,
            MembroId = c.MembroId,
            Ativo = c.Ativo,
            TransacoesVinculadas = c.MetaTransacoes.Select(mt => new MetaTransacaoResponse
            {
                Id = mt.Id,
                MetaId = mt.MetaId,
                TransacaoId = mt.TransacaoId,
                VinculadoEm = mt.VinculadoEm
            }).ToList()
        });
    }
}