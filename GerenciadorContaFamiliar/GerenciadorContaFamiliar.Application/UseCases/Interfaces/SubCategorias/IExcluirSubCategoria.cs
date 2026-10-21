using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;

public interface IExcluirSubCategoriaUseCase
{
    Task<SubCategoriaResponse> ExecutarAsync(
        ExcluirSubCategoriaRequest request,
        CancellationToken cancellationToken = default);
}
