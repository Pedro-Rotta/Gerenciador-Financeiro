using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;

public interface IListarContaConjuntaUseCase
{
    Task<IEnumerable<ContaConjuntaResponse>> ExecutarAsync(
        ListarContaConjuntaRequest request,
        CancellationToken ct = default);
}
