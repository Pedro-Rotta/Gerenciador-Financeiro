using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;

public interface IListarConvitesRecebidosUseCase
{
    Task<IEnumerable<ConviteResponse>> ExecutarAsync(
        ListarConvitesRecebidosRequest request,
        CancellationToken ct = default);
}
