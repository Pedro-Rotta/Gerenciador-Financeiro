using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;

public interface ICriarSubCategoriaUseCase
{
    Task<SubCategoriaResponse> ExecutarAsync(
        CriarSubCategoriaRequest request,
        CancellationToken cancellationToken = default);
}
