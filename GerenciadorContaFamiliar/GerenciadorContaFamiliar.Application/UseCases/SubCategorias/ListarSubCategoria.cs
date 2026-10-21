using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.SubCategorias;

public class ListarSubCategoriaUseCase : IListarSubCategoriaUseCase
{
    private readonly ISubCategoriaRepository _subCategoriaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;

    public ListarSubCategoriaUseCase(
        ISubCategoriaRepository subCategoriaRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository)
    {
        _subCategoriaRepository = subCategoriaRepository
            ?? throw new ArgumentNullException(nameof(subCategoriaRepository));

        _contaRepository = contaRepository
            ?? throw new ArgumentNullException(nameof(contaRepository));

        _contaConjuntaRepository = contaConjuntaRepository
            ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
    }

    public async Task<IEnumerable<SubCategoriaResponse>> ExecutarAsync(
        ListarSubCategoriaRequest request,
        CancellationToken ct = default)
    {
        // 1. Validação básica do request
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        bool temConta = request.ContaId.HasValue;
        bool temContaConjunta = request.ContaConjuntaId.HasValue;

        // Exatamente um dos dois deve ser informado
        if (temConta == temContaConjunta)
            throw new ArgumentException(
                "Informe exatamente um: ContaId OU ContaConjuntaId");

        // 2. Conta normal
        if (temConta)
        {
            var conta = await _contaRepository
                .ObterPorIdAsync(request.ContaId!.Value, ct)
                ?? throw new KeyNotFoundException("Conta não encontrada.");

            // Validação de propriedade
            if (conta.MembroId != request.MembroId)
                throw new AcessoNegadoException(
                    "Você não tem permissão para acessar esta conta.");

            var subCategorias = await _subCategoriaRepository
                .ObterPorContaAsync(request.ContaId.Value, ct);

            return subCategorias.Select(c => new SubCategoriaResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Ativo = c.Ativo,
                ContaId = c.ContaId,
                ContaConjuntaId = c.ContaConjuntaId
            });
        }

        // 3. Conta conjunta
        var contaConjunta = await _contaConjuntaRepository
            .ObterPorIdAsync(request.ContaConjuntaId!.Value, ct)
            ?? throw new KeyNotFoundException(
                "Conta conjunta não encontrada.");

        // Validação de pertencimento
        if (!contaConjunta.Membros.Any(m => m.Id == request.MembroId))
            throw new AcessoNegadoException(
                "Você não tem permissão para acessar esta conta conjunta.");

        var subCategoriasConjuntas = await _subCategoriaRepository
            .ObterPorContaConjuntaAsync(request.ContaConjuntaId.Value, ct);
        return subCategoriasConjuntas.Select(c => new SubCategoriaResponse
        {
            Id = c.Id,
            Nome = c.Nome,
            Ativo = c.Ativo,
            ContaId = c.ContaId,
            ContaConjuntaId = c.ContaConjuntaId
        });
    }
}
