using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;

public interface IDesvincularSubCategoriaUseCase
{
    Task<CategoriaResponse> ExecutarAsync(
        DesvincularSubCategoriaRequest request,
        CancellationToken cancellationToken = default);
}
