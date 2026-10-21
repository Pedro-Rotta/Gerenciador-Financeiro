using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Metas;

public class DesvincularTransacaoMetaUseCase : IDesvincularTransacaoMetaUseCase
{
    private readonly IMetaRepository _metaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IMetaTransacaoRepository _metaTransacaoRepository;
    private readonly IProgressoDaMeta _progressoDaMeta;
    private readonly IUnitOfWork _unitOfWork;

    public DesvincularTransacaoMetaUseCase(
        IMetaRepository metaRepository,
        ITransacaoRepository transacaoRepository,
        IMetaTransacaoRepository metaTransacaoRepository,
        IProgressoDaMeta progressoDaMeta,
        IUnitOfWork unitOfWork)
    {
        _metaRepository = metaRepository ?? throw new ArgumentNullException(nameof(metaRepository));
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _metaTransacaoRepository = metaTransacaoRepository ?? throw new ArgumentNullException(nameof(metaTransacaoRepository));
        _progressoDaMeta = progressoDaMeta ?? throw new ArgumentNullException(nameof(progressoDaMeta));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MetaResponse> ExecutarAsync(
        DesvincularTransacaoMetaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a meta (com transações vinculadas)
        var meta = await _metaRepository.ObterComTransacoesAsync(request.MetaId, cancellationToken)
            ?? throw new KeyNotFoundException("Meta não encontrada.");

        // Verificar propriedade: só o dono pode alterar a meta
        if (meta.MembroId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para alterar esta meta.");

        if (!meta.Ativo)
            throw new InvalidOperationException("Meta está inativa.");

        // 2. Buscar a vinculação existente
        var metaTransacao = await _metaTransacaoRepository.ObterPorMetaETransacaoAsync(request.MetaId, request.TransacaoId, cancellationToken)
            ?? throw new KeyNotFoundException("Vinculação não encontrada. A transação não está vinculada a esta meta.");

        // 3. Buscar a transação para obter o valor
        var transacao = await _transacaoRepository.ObterPorIdAsync(request.TransacaoId, cancellationToken)
            ?? throw new KeyNotFoundException("Transação não encontrada.");

        // 4. Desvincular (domínio)
        meta.DesvincularTransacao(metaTransacao);

        // 5. Recalcular o ValorAtual sem a transação removida
        await _progressoDaMeta.RecalcularAsync(meta, ignorarTransacaoId: transacao.Id, ct: cancellationToken);

        // 6. Persistir
        await _metaTransacaoRepository.RemoverAsync(metaTransacao.Id, cancellationToken);
        await _metaRepository.AtualizarAsync(meta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Mapear resposta (usa as vinculações restantes na meta)
        return MapearParaResponse(meta, meta.MetaTransacoes.ToList());
    }

    private MetaResponse MapearParaResponse(Meta meta, IReadOnlyList<MetaTransacao> vinculacoes)
    {
        return new MetaResponse
        {
            Id = meta.Id,
            Nome = meta.Nome,
            ValorAlvo = meta.ValorAlvo,
            ValorAtual = meta.ValorAtual,
            Prazo = meta.Prazo,
            Concluida = meta.Concluida,
            MembroId = meta.MembroId,
            Ativo = meta.Ativo,
            TransacoesVinculadas = vinculacoes.Select(v => new MetaTransacaoResponse
            {
                Id = v.Id,
                MetaId = v.MetaId,
                TransacaoId = v.TransacaoId,
                VinculadoEm = v.VinculadoEm
            }).ToList()
        };
    }
}
