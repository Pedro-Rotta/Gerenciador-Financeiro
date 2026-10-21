using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;

public interface IExcluirCategoriaUseCase
{
    Task<CategoriaResponse> ExecutarAsync(
        ExcluirCategoriaRequest request, 
        CancellationToken cancellationToken = default);
}