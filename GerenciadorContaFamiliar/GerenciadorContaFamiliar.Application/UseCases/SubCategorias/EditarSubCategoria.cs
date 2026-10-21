using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.SubCategorias;

public class EditarSubCategoriaUseCase : IEditarSubCategoriaUseCase
{
    private readonly ISubCategoriaRepository _subCategoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EditarSubCategoriaUseCase(
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
        EditarSubCategoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var subCategoria = await _subCategoriaRepository.ObterPorIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Subcategoria não encontrada.");

        if (!subCategoria.Ativo)
            throw new InvalidOperationException("Subcategoria já está inativa.");

        // 0. Verificar propriedade: subcategoria pertence a Conta do membro ou ContaConjunta do membro
        if (subCategoria.ContaId.HasValue)
        {
            var conta = await _contaRepository.ObterPorIdAsync(subCategoria.ContaId.Value, cancellationToken);
            if (conta is null || conta.MembroId != request.MembroId)
                throw new AcessoNegadoException("Você não tem permissão para editar esta subcategoria.");
        }
        else if (subCategoria.ContaConjuntaId.HasValue)
        {
            var membros = await _contaConjuntaRepository.ObterMembrosDaContaConjuntaAsync(subCategoria.ContaConjuntaId.Value, cancellationToken);
            if (!membros.Any(m => m.Id == request.MembroId))
                throw new AcessoNegadoException("Você não tem permissão para editar esta subcategoria.");
        }
        else
        {
            throw new InvalidOperationException("Subcategoria não está vinculada a nenhuma conta.");
        }

        // 2. Atualizar somente o que foi enviado
        if (request.Nome is not null && request.Nome != subCategoria.Nome)
        {
            // Duplicidade verificada só dentro da MESMA conta (individual ou conjunta)
            var existente = await _subCategoriaRepository.ObterPorNomeAsync(
                request.Nome, subCategoria.ContaId, subCategoria.ContaConjuntaId, cancellationToken);
            if (existente is not null && existente.Id != request.Id)
                throw new ArgumentException("Já existe uma subcategoria com este nome nesta conta.");
            subCategoria.AtualizarNome(request.Nome);
        }

        // 3. Persistir
        await _subCategoriaRepository.AtualizarAsync(subCategoria, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Mapear resposta
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
