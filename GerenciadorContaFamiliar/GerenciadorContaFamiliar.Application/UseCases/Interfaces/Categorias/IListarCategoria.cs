using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;

public interface IListarCategoriaUseCase
{
    Task<IEnumerable<CategoriaResponse>> ExecutarAsync(
        ListarCategoriaRequest request, 
        CancellationToken ct = default);
}