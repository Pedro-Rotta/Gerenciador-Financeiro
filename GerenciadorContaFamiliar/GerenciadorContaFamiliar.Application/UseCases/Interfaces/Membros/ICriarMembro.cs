using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;

public interface ICriarMembroUseCase
{
    Task<MembroResponse> ExecutarAsync(
        CriarMembroRequest request, 
        CancellationToken cancellationToken = default);
}