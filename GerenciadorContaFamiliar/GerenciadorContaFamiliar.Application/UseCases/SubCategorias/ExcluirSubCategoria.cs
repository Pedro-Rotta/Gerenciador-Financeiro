using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.SubCategorias;

public class ExcluirSubCategoriaUseCase : IExcluirSubCategoriaUseCase
{
    private readonly ISubCategoriaRepository _subCategoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExcluirSubCategoriaUseCase(
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
        ExcluirSubCategoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var subCategoria = await _subCategoriaRepository.ObterPorIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Subcategoria não encontrada.");

        // 0. Verificar propriedade: subcategoria pertence a Conta do membro ou ContaConjunta do membro
        if (subCategoria.ContaId.HasValue)
        {
            var conta = await _contaRepository.ObterPorIdAsync(subCategoria.ContaId.Value, cancellationToken);
            if (conta is null || conta.MembroId != request.MembroId)
                throw new AcessoNegadoException("Você não tem permissão para excluir esta subcategoria.");
        }
        else if (subCategoria.ContaConjuntaId.HasValue)
        {
            var membros = await _contaConjuntaRepository.ObterMembrosDaContaConjuntaAsync(subCategoria.ContaConjuntaId.Value, cancellationToken);
            if (!membros.Any(m => m.Id == request.MembroId))
                throw new AcessoNegadoException("Você não tem permissão para excluir esta subcategoria.");
        }
        else
        {
            throw new InvalidOperationException("Subcategoria não está vinculada a nenhuma conta.");
        }

        if (!subCategoria.Ativo)
            throw new InvalidOperationException("Subcategoria já inativa.");

        // 2. Soft delete. O trigger do banco remove os vínculos com categorias;
        //    as transações continuam ativas (passam a aparecer sem subcategoria).
        subCategoria.Inativar();

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
