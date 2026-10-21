using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Categorias;

public class ListarCategoriaUseCase : IListarCategoriaUseCase
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;

    public ListarCategoriaUseCase(
        ICategoriaRepository categoriaRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository)
    {
        _categoriaRepository = categoriaRepository;
        _contaRepository = contaRepository;
        _contaConjuntaRepository = contaConjuntaRepository;
    }

    public async Task<IEnumerable<CategoriaResponse>> ExecutarAsync(
        ListarCategoriaRequest request, 
        CancellationToken ct = default)
    {
        // 1. Validação básica do request
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        bool temConta = request.ContaId.HasValue;
        bool temContaConjunta = request.ContaConjuntaId.HasValue;

        // XOR: exatamente um dos dois deve ser preenchido
        if (temConta == temContaConjunta) 
            throw new ArgumentException("Informe exatamente um: ContaId OU ContaConjuntaId");

        // 2. Busca a conta/conta conjunta para validar pertencimento ao usuário
        if (temConta)
        {
            var conta = await _contaRepository.ObterPorIdAsync(request.ContaId!.Value, ct)
                ?? throw new KeyNotFoundException("Conta não encontrada.");

            // Validação de propriedade: só o dono pode acessar
            if (conta.MembroId != request.MembroId)
                throw new AcessoNegadoException("Você não tem permissão para acessar esta conta.");

            // Busca categorias vinculadas a esta conta
            var categorias = await _categoriaRepository.ObterPorContaAsync(request.ContaId.Value, ct);
            return categorias.Select(c => new CategoriaResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Tipo = c.Tipo,
                Necessario = c.Necessario,
                Icone = c.Icone,
                Ativo = c.Ativo,
                ContaId = c.ContaId,
                ContaConjuntaId = c.ContaConjuntaId,
                SubCategorias = c.SubCategorias
                    .Select(s => new SubCategoriaResponse { Id = s.Id, Nome = s.Nome, Ativo = s.Ativo, ContaId = s.ContaId, ContaConjuntaId = s.ContaConjuntaId })
                    .ToList(),
            });
        }
        else // temContaConjunta
        {
            var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.ContaConjuntaId!.Value, ct)
                ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");

            // Validação de propriedade: só o dono pode acessar
            if (!contaConjunta.Membros.Any(m => m.Id == request.MembroId))
                throw new AcessoNegadoException("Você não tem permissão para acessar esta conta conjunta.");

            // Busca categorias vinculadas a esta conta conjunta
            var categorias = await _categoriaRepository.ObterPorContaConjuntaAsync(request.ContaConjuntaId.Value, ct);
            return categorias.Select(c => new CategoriaResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Tipo = c.Tipo,
                Necessario = c.Necessario,
                Icone = c.Icone,
                Ativo = c.Ativo,
                ContaId = c.ContaId,
                ContaConjuntaId = c.ContaConjuntaId,
                SubCategorias = c.SubCategorias
                    .Select(s => new SubCategoriaResponse { Id = s.Id, Nome = s.Nome, Ativo = s.Ativo, ContaId = s.ContaId, ContaConjuntaId = s.ContaConjuntaId })
                    .ToList(),
            });
        }
    }
}