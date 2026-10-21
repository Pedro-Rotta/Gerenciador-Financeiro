using GerenciadorContaFamiliar.Application.DTOs.Requests.Recorrencias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class RecorrenciaController : BaseApiController
{
    private readonly IListarOcorrenciasPrevistasUseCase _listarPrevistasUseCase;

    public RecorrenciaController(IListarOcorrenciasPrevistasUseCase listarPrevistasUseCase)
    {
        _listarPrevistasUseCase = listarPrevistasUseCase ?? throw new ArgumentNullException(nameof(listarPrevistasUseCase));
    }

    /// <summary>Prévia das ocorrências mensais até o fim do próximo mês (não entram no saldo).</summary>
    [HttpGet("previstas")]
    public async Task<ActionResult<IEnumerable<OcorrenciaPrevistaResponse>>> ListarPrevistas(CancellationToken ct = default)
    {
        var response = await _listarPrevistasUseCase.ExecutarAsync(
            new ListarOcorrenciasPrevistasRequest { MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }
}
