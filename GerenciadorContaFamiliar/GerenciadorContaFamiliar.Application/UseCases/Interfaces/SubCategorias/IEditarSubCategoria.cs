using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;

public interface IEditarSubCategoriaUseCase
{
    Task<SubCategoriaResponse> ExecutarAsync(
        EditarSubCategoriaRequest request,
        CancellationToken cancellationToken = default);
}
