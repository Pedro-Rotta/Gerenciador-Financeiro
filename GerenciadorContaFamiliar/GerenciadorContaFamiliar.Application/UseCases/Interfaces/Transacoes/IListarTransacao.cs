using GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;

public interface IListarTransacaoUseCase
{
    Task<IEnumerable<TransacaoResponse>> ExecutarAsync(
        ListarTransacaoRequest request,
        CancellationToken ct = default);
}
