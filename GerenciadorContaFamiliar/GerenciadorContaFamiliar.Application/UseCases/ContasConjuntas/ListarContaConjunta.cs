using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.ContasConjuntas;

public class ListarContaConjuntaUseCase : IListarContaConjuntaUseCase
{
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly ITransacaoRepository _transacaoRepository;

    public ListarContaConjuntaUseCase(
        IContaConjuntaRepository contaConjuntaRepository,
        ITransacaoRepository transacaoRepository)
    {
        _contaConjuntaRepository = contaConjuntaRepository;
        _transacaoRepository = transacaoRepository ?? throw new ArgumentNullException(nameof(transacaoRepository));
    }

    public async Task<IEnumerable<ContaConjuntaResponse>> ExecutarAsync(
        ListarContaConjuntaRequest request,
        CancellationToken ct = default)
    {
        var contas = await _contaConjuntaRepository.ObterPorMembroAsync(request.MembroId, ct);

        var resposta = new List<ContaConjuntaResponse>();
        foreach (var c in contas)
            resposta.Add(new ContaConjuntaResponse
            {
                Id = c.Id,
                Nome = c.Nome,
                Saldo = c.Saldo,
                // Previsto: saldo + o que ainda está pendente ou atrasado
                SaldoPrevisto = c.Saldo + await _transacaoRepository.SomarEmAbertoAsync(null, c.Id, ct),
                Descricao = c.Descricao,
                CriadorId = c.CriadorId,
                Ativo = c.Ativo,
                Membros = c.Membros
                    .Select(m => new MembroResumoResponse { Id = m.Id, Nome = m.Nome, NomeDeUsuario = m.NomeDeUsuario })
                    .ToList(),
            });
        return resposta;
    }
}
