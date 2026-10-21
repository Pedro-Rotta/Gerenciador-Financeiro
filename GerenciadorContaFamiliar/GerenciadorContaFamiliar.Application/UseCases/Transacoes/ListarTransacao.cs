using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;
using System.Collections.Generic;
using System.Linq;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Transacoes;

public class ListarTransacaoUseCase : IListarTransacaoUseCase
{
    private readonly ITransacaoRepository _transacaoRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;

    public ListarTransacaoUseCase(
            ITransacaoRepository transacaoRepository,
            IContaRepository contaRepository,
            IContaConjuntaRepository contaConjuntaRepository
        )
    {
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
    }

    public async Task<IEnumerable<TransacaoResponse>> ExecutarAsync(
        ListarTransacaoRequest request, CancellationToken ct = default)
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

            // Busca transacoes vinculadas a esta conta (apenas da conta principal, sem conjuntas)
            var transacoes = await _transacaoRepository.ObterPorContaAsync(request.ContaId.Value, ct);
            return transacoes.Select(c => new TransacaoResponse
            {
                Id = c.Id,
                Valor = c.Valor,
                Descricao = c.Descricao,
                DataTransacao = c.DataTransacao,
                Tipo = c.Tipo,
                Classificacao = c.Classificacao,
                Recorrente = c.Recorrente,
                Periodicidade = c.Periodicidade,
                Status = c.Status,
                MembroId = c.Membro.Id,
                CategoriaId = c.Categoria?.Id,
                SubCategoriaId = c.SubCategoria != null ? c.SubCategoria.Id : (int?)null,
                ContaId = c.Conta != null ? c.Conta.Id : (int?)null,
                ContaConjuntaId = c.ContaConjunta != null ? c.ContaConjunta.Id : (int?)null,
                RecorrenciaId = c.Recorrencia?.Id,
                NumeroOcorrencia = c.NumeroOcorrencia,
                TotalOcorrencias = c.Recorrencia?.TotalOcorrencias,
                SerieAtiva = c.Recorrencia?.Ativa,
                CriadoEm = c.CriadoEm,
                Ativo = c.Ativo,
            });
        }
        else // temContaConjunta
        {
            var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.ContaConjuntaId!.Value, ct)
                ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");

            // Validação de propriedade: só o dono pode acessar
            if (!contaConjunta.Membros.Any(m => m.Id == request.MembroId))
                throw new AcessoNegadoException("Você não tem permissão para acessar esta conta conjunta.");

            // Busca transacoes vinculadas a esta conta conjunta
            var transacoes = await _transacaoRepository.ObterPorContaConjuntaAsync(request.ContaConjuntaId.Value, ct);
            return transacoes.Select(c => new TransacaoResponse
            {
                Id = c.Id,
                Valor = c.Valor,
                Descricao = c.Descricao,
                DataTransacao = c.DataTransacao,
                Tipo = c.Tipo,
                Classificacao = c.Classificacao,
                Recorrente = c.Recorrente,
                Periodicidade = c.Periodicidade,
                Status = c.Status,
                MembroId = c.Membro.Id,
                CategoriaId = c.Categoria?.Id,
                SubCategoriaId = c.SubCategoria != null ? c.SubCategoria.Id : (int?)null,
                ContaId = c.Conta != null ? c.Conta.Id : (int?)null,
                ContaConjuntaId = c.ContaConjunta != null ? c.ContaConjunta.Id : (int?)null,
                RecorrenciaId = c.Recorrencia?.Id,
                NumeroOcorrencia = c.NumeroOcorrencia,
                TotalOcorrencias = c.Recorrencia?.TotalOcorrencias,
                SerieAtiva = c.Recorrencia?.Ativa,
                CriadoEm = c.CriadoEm,
                Ativo = c.Ativo,
            });
        }
    }
}
