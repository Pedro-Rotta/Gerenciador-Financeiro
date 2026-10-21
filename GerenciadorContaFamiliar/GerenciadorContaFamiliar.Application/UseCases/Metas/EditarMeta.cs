using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Metas;

public class EditarMetaUseCase : IEditarMetaUseCase
{
    private readonly IMetaRepository _metaRepository;
    private readonly IProgressoDaMeta _progressoDaMeta;
    private readonly IUnitOfWork _unitOfWork;

    public EditarMetaUseCase(IMetaRepository metaRepository, IProgressoDaMeta progressoDaMeta, IUnitOfWork unitOfWork)
    {
        _metaRepository = metaRepository ?? throw new ArgumentNullException(nameof(metaRepository));
        _progressoDaMeta = progressoDaMeta ?? throw new ArgumentNullException(nameof(progressoDaMeta));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MetaResponse> ExecutarAsync(
        EditarMetaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var meta = await _metaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Meta não encontrada.");

        // 0. Verificar propriedade: só o dono pode editar
        if (meta.MembroId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para editar esta meta.");

        // 2. Atualizar somente o que foi enviado
        if (request.Nome is not null)
            meta.AtualizarNome(request.Nome);
        // Novo alvo: "concluída" passa a valer pela soma real das vinculadas (sem aviso:
        // foi o próprio dono que mudou)
        if (request.ValorAlvo is not null && request.ValorAlvo.Value != meta.ValorAlvo)
        {
            meta.AtualizarValorAlvo(request.ValorAlvo.Value);
            await _progressoDaMeta.RecalcularAsync(meta, ct: ct);
        }
        // Só valida/atualiza o prazo quando ele muda: assim uma meta vencida
        // ainda pode ser renomeada ou ter o valor alvo ajustado.
        if (request.Prazo is not null && request.Prazo.Value != meta.Prazo)
            meta.AtualizarPrazo(request.Prazo.Value);

        // 3. Persistir
        await _metaRepository.AtualizarAsync(meta, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. Mapear resposta
        return new MetaResponse
        {
            Id = meta.Id,
            Nome = meta.Nome,
            ValorAlvo = meta.ValorAlvo,
            ValorAtual = meta.ValorAtual,
            Prazo = meta.Prazo,
            Concluida = meta.Concluida,
            MembroId = meta.MembroId,
            Ativo = meta.Ativo
        };
    }
}
