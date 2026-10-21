using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Metas;

public class ExcluirMetaUseCase : IExcluirMetaUseCase
{
    private readonly IMetaRepository _metaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExcluirMetaUseCase(IMetaRepository metaRepository, IUnitOfWork unitOfWork)
    {
        _metaRepository = metaRepository ?? throw new ArgumentNullException(nameof(metaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MetaResponse> ExecutarAsync(
        ExcluirMetaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var meta = await _metaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Meta não encontrada.");

        // 0. Verificar propriedade: só o dono pode excluir
        if (meta.MembroId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para excluir esta meta.");

        if (!meta.Ativo)
            throw new InvalidOperationException("Meta já está inativa.");

        // 2. Soft delete (inativa)
        meta.Inativar();

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
