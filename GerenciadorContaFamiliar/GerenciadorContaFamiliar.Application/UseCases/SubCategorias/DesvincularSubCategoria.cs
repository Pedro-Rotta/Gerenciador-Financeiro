using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.SubCategorias;

public class DesvincularSubCategoriaUseCase : IDesvincularSubCategoriaUseCase
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DesvincularSubCategoriaUseCase(
        ICategoriaRepository categoriaRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        IUnitOfWork unitOfWork)
    {
        _categoriaRepository = categoriaRepository ?? throw new ArgumentNullException(nameof(categoriaRepository));
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CategoriaResponse> ExecutarAsync(
        DesvincularSubCategoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a categoria (com as subcategorias já vinculadas)
        var categoria = await _categoriaRepository.ObterComSubCategoriasAsync(request.IdCategoria, cancellationToken)
            ?? throw new KeyNotFoundException("Categoria não encontrada.");

        if (!categoria.Ativo)
            throw new InvalidOperationException("Categoria já está inativa.");

        await ValidarPertencimentoAsync(categoria, request.MembroId, cancellationToken);

        // 2. Desvincular (idempotente — não vinculada = nada a fazer)
        var subCategoria = categoria.SubCategorias
            .FirstOrDefault(s => s.Id == request.IdSubCategoria)
            ?? throw new ArgumentException("A subcategoria não está vinculada a esta categoria.");

        categoria.DesvincularSubCategoria(subCategoria);

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
