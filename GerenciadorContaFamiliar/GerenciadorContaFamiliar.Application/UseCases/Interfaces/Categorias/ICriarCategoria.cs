using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;

public interface ICriarCategoriaUseCase
{
    Task<CategoriaResponse> ExecutarAsync(
        CriarCategoriaRequest request, 
        CancellationToken cancellationToken = default);
}