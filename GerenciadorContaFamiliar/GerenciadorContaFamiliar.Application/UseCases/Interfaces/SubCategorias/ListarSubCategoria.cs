using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;

public interface IListarSubCategoriaUseCase
{
    Task<IEnumerable<SubCategoriaResponse>> ExecutarAsync(
        ListarSubCategoriaRequest request,
        CancellationToken ct = default);
}
