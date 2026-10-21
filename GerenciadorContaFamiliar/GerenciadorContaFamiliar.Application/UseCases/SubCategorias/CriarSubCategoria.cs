using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.SubCategorias;

public class CriarSubCategoriaUseCase : ICriarSubCategoriaUseCase
{
    private readonly ISubCategoriaRepository _subCategoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarSubCategoriaUseCase(
        ISubCategoriaRepository subCategoriaRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        IUnitOfWork unitOfWork)
    {
        _subCategoriaRepository = subCategoriaRepository ?? throw new ArgumentNullException(nameof(subCategoriaRepository));
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SubCategoriaResponse> ExecutarAsync(
        CriarSubCategoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // Validação XOR: exatamente uma das duas FKs deve ser informada
        if ((request.ContaId.HasValue && request.ContaConjuntaId.HasValue) ||
            (!request.ContaId.HasValue && !request.ContaConjuntaId.HasValue))
            throw new ArgumentException(
                "A subcategoria deve estar vinculada a exatamente uma Conta (ContaId) ou ContaConjunta (ContaConjuntaId).");

        // 0. Verificar propriedade: a conta precisa ser do membro (ou conjunta da qual ele participa)
        if (request.ContaId.HasValue)
        {
            var conta = await _contaRepository.ObterPorIdAsync(request.ContaId.Value, cancellationToken)
                ?? throw new ArgumentException("Conta não encontrada.");
            if (conta.MembroId != request.MembroId)
                throw new AcessoNegadoException("Você não tem permissão para criar subcategorias nesta conta.");
        }
        else
        {
            var membros = await _contaConjuntaRepository.ObterMembrosDaContaConjuntaAsync(request.ContaConjuntaId!.Value, cancellationToken);
            if (!membros.Any(m => m.Id == request.MembroId))
                throw new AcessoNegadoException("Você não tem permissão para criar subcategorias nesta conta conjunta.");
        }

        // 1️⃣ Validação de entrada — verifica duplicidade de nome dentro da MESMA conta
        // (individual via ContaId OU conjunta via ContaConjuntaId).
        var existente = await _subCategoriaRepository.ObterPorNomeAsync(
            request.Nome,
            request.ContaId,
            request.ContaConjuntaId,
            cancellationToken);
        if (existente is not null)
            throw new ArgumentException("Já existe uma subcategoria com este nome nesta conta.");

        // 2️⃣ Criar entidade de domínio (rótulo independente — sem categoria fixa)
        var subCategoria = new SubCategoria(request.Nome);

        // Vincular à Conta ou ContaConjunta
        if (request.ContaId.HasValue)
            subCategoria.DefinirConta(request.ContaId.Value);
        else
            subCategoria.DefinirContaConjunta(request.ContaConjuntaId!.Value);

        // 3️⃣ Persistir via repositório
        await _subCategoriaRepository.AdicionarAsync(subCategoria, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4️⃣ Mapear para DTO de saída
        return new SubCategoriaResponse
        {
            Id = subCategoria.Id,
            Nome = subCategoria.Nome,
            Ativo = subCategoria.Ativo,
            ContaId = subCategoria.ContaId,
            ContaConjuntaId = subCategoria.ContaConjuntaId,
        };
    }
}
