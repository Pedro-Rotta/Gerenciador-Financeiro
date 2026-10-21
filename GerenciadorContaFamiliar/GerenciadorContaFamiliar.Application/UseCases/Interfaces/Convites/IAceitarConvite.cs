using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;

public interface IAceitarConviteUseCase
{
    Task<ConviteResponse> ExecutarAsync(
        AceitarConviteRequest request,
        CancellationToken ct = default);
}
