using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Threading;
using System.Threading.Tasks;
namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;

public interface IEditarCategoriaUseCase
{
    Task<CategoriaResponse> ExecutarAsync(
        EditarCategoriaRequest request, 
        CancellationToken cancellationToken = default);
}