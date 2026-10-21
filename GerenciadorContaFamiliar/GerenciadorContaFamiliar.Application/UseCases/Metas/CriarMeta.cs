using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;

namespace GerenciadorContaFamiliar.Application.UseCases.Metas;

public class CriarMetaUseCase : ICriarMetaUseCase
{
    private readonly IMetaRepository _metaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarMetaUseCase(IMetaRepository metaRepository, IUnitOfWork unitOfWork)
    {
        _metaRepository = metaRepository ?? throw new ArgumentNullException(nameof(metaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MetaResponse> ExecutarAsync(
        CriarMetaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1️⃣ Criar entidade de domínio (valida nome, valor alvo e prazo)
        var meta = new Meta(
            request.Nome,
            request.ValorAlvo,
            request.Prazo,
            request.MembroId
        );

        // 2️⃣ Persistir via repositório
        await _metaRepository.AdicionarAsync(meta, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 3️⃣ Mapear para DTO de saída
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
