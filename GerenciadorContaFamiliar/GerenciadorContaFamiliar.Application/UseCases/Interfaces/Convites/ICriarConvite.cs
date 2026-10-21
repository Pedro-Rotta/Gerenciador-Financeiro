using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;

public interface ICriarConviteUseCase
{
    Task<ConviteResponse> ExecutarAsync(
        CriarConviteRequest request,
        CancellationToken cancellationToken = default);
}
