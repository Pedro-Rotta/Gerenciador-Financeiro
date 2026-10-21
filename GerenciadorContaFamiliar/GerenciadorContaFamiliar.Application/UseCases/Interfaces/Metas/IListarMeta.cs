using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;

public interface IListarMetaUseCase
{
    Task<IEnumerable<MetaResponse>> ExecutarAsync(
        ListarMetaRequest request,
        CancellationToken ct = default);
}
