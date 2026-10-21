using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Metas;

public class VincularTransacaoMetaUseCase : IVincularTransacaoMetaUseCase
{
    private readonly IMetaRepository _metaRepository;
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IMetaTransacaoRepository _metaTransacaoRepository;
    private readonly IProgressoDaMeta _progressoDaMeta;
    private readonly IUnitOfWork _unitOfWork;

    public VincularTransacaoMetaUseCase(
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
        VincularTransacaoMetaRequest request,
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

        // 2. Buscar a transação
        var transacao = await _transacaoRepository.ObterPorIdAsync(request.TransacaoId, cancellationToken)
            ?? throw new KeyNotFoundException("Transação não encontrada.");

        // Verificar propriedade: só o autor da transação pode vinculá-la a uma meta
        if (transacao.Membro.Id != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para vincular esta transação.");

        if (!transacao.Ativo)
            throw new InvalidOperationException("Transação está inativa.");

        // 3. Verificar se a transação já está vinculada a alguma meta
        if (await _metaTransacaoRepository.TransacaoJaVinculadaAsync(request.TransacaoId, cancellationToken))
            throw new InvalidOperationException("Esta transação já está vinculada a uma meta.");

        // 4. Criar a vinculação
        var metaTransacao = new MetaTransacao(request.MetaId, request.TransacaoId);

        // 5. Vincular à meta (domínio)
        meta.VincularTransacao(metaTransacao);

        // 6. Recalcular o ValorAtual (vinculadas concluídas, já com esta) e avisar o dono do
        // progresso, se ele ligou "Progresso da meta"
        await _progressoDaMeta.RecalcularAsync(
            meta, incluir: transacao, avisarDono: transacao.Membro, ct: cancellationToken);

        // 7. Persistir
        await _metaTransacaoRepository.AdicionarAsync(metaTransacao, cancellationToken);
        await _metaRepository.AtualizarAsync(meta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. Mapear resposta (usa as vinculações carregadas na meta)
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
