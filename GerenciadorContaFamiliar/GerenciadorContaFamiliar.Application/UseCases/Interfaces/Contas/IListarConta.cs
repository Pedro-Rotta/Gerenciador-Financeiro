using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;

public interface IListarContaUseCase
{
    Task<IEnumerable<ContaResponse>> ExecutarAsync(
        ListarContaRequest request,
        CancellationToken ct = default);
}
