using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;

public interface IVincularSubCategoriaUseCase
{
    Task<CategoriaResponse> ExecutarAsync(
        VincularSubCategoriaRequest request,
        CancellationToken cancellationToken = default);
}
