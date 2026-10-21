using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.SubCategorias;

public class VincularSubCategoriaUseCase : IVincularSubCategoriaUseCase
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly ISubCategoriaRepository _subCategoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VincularSubCategoriaUseCase(
        ICategoriaRepository categoriaRepository,
        ISubCategoriaRepository subCategoriaRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        IUnitOfWork unitOfWork)
    {
        _categoriaRepository = categoriaRepository ?? throw new ArgumentNullException(nameof(categoriaRepository));
        _subCategoriaRepository = subCategoriaRepository ?? throw new ArgumentNullException(nameof(subCategoriaRepository));
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CategoriaResponse> ExecutarAsync(
        VincularSubCategoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a categoria (com as subcategorias já vinculadas) e a subcategoria
        var categoria = await _categoriaRepository.ObterComSubCategoriasAsync(request.IdCategoria, cancellationToken)
            ?? throw new KeyNotFoundException("Categoria não encontrada.");

        if (!categoria.Ativo)
            throw new InvalidOperationException("Categoria já está inativa.");

        await ValidarPertencimentoAsync(categoria, request.MembroId, cancellationToken);

        var subCategoria = await _subCategoriaRepository.ObterPorIdAsync(request.IdSubCategoria, cancellationToken)
            ?? throw new KeyNotFoundException("Subcategoria não encontrada.");

        if (!subCategoria.Ativo)
            throw new InvalidOperationException("Subcategoria já está inativa.");

        // 2. Vincular (idempotente — o domínio ignora vínculo duplicado)
        // Validar que categoria e subcategoria pertencem à mesma conta
        if (categoria.ContaId.HasValue && categoria.ContaId != subCategoria.ContaId)
            throw new ArgumentException("A subcategoria não pertence à mesma conta da categoria.");
        if (categoria.ContaConjuntaId.HasValue && categoria.ContaConjuntaId != subCategoria.ContaConjuntaId)
            throw new ArgumentException("A subcategoria não pertence à mesma conta conjunta da categoria.");

        categoria.VincularSubCategoria(subCategoria);

        // 3. Persistir
        await _categoriaRepository.AtualizarAsync(categoria, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Mapear resposta (inclui as subcategorias vinculadas atualizadas)
        return new CategoriaResponse
        {
            Id = categoria.Id,
            Nome = categoria.Nome,
            Tipo = categoria.Tipo,
            Necessario = categoria.Necessario,
            Icone = categoria.Icone,
            Ativo = categoria.Ativo,
            ContaId = categoria.ContaId,
            ContaConjuntaId = categoria.ContaConjuntaId,
            SubCategorias = categoria.SubCategorias
                .Select(s => new SubCategoriaResponse { Id = s.Id, Nome = s.Nome, Ativo = s.Ativo, ContaId = s.ContaId, ContaConjuntaId = s.ContaConjuntaId })
                .ToList(),
        };
    }

    private async Task ValidarPertencimentoAsync(Categoria categoria, int membroId, CancellationToken ct)
    {
        if (categoria.ContaId.HasValue)
        {
            var conta = await _contaRepository.ObterPorIdAsync(categoria.ContaId.Value, ct)
                ?? throw new KeyNotFoundException("Conta não encontrada.");
            if (conta.MembroId != membroId)
                throw new AcessoNegadoException("Você não tem permissão para acessar esta categoria.");
        }
        else if (categoria.ContaConjuntaId.HasValue)
        {
            var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(categoria.ContaConjuntaId.Value, ct)
                ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");
            if (!contaConjunta.Membros.Any(m => m.Id == membroId))
                throw new AcessoNegadoException("Você não tem permissão para acessar esta categoria.");
        }
    }
}
