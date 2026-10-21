using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Contas;

public class ListarContaUseCase : IListarContaUseCase
{
    private readonly IContaRepository _contaRepository;
    private readonly ITransacaoRepository _transacaoRepository;

    public ListarContaUseCase(IContaRepository contaRepository, ITransacaoRepository transacaoRepository)
    {
        _contaRepository = contaRepository;
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
    }

    public async Task<IEnumerable<ContaResponse>> ExecutarAsync(
        ListarContaRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        var contas = await _contaRepository.ObterPorMembroAsync(request.MembroId, ct);

        var resposta = new List<ContaResponse>();
        foreach (var c in contas)
            resposta.Add(new ContaResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Saldo = c.Saldo,
                // Previsto: saldo + o que ainda está pendente ou atrasado
                SaldoPrevisto = c.Saldo + await _transacaoRepository.SomarEmAbertoAsync(c.Id, null, ct),
                SaldoInicialDefinido = c.SaldoInicialDefinido,
                MembroId = c.MembroId,
                Ativo = c.Ativo,
                ContaPrincipal = c.EhContaPrincipal,
            });
        return resposta;
    }
}
