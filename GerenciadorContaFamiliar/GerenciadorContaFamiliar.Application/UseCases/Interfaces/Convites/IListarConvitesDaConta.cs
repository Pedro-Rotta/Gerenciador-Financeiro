using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;

public interface IListarConvitesDaContaUseCase
{
    Task<IEnumerable<ConviteResponse>> ExecutarAsync(
        ListarConvitesDaContaRequest request,
        CancellationToken ct = default);
}
