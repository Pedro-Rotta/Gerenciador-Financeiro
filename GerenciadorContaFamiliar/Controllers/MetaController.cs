using GerenciadorContaFamiliar.Application.DTOs.Requests.Metas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Metas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class MetaController : BaseApiController
{
    private readonly ICriarMetaUseCase _criarMetaUseCase;
    private readonly IEditarMetaUseCase _editarMetaUseCase;
    private readonly IExcluirMetaUseCase _excluirMetaUseCase;
    private readonly IListarMetaUseCase _listarMetaUseCase;
    private readonly IVincularTransacaoMetaUseCase _vincularTransacaoMetaUseCase;
    private readonly IDesvincularTransacaoMetaUseCase _desvincularTransacaoMetaUseCase;

    public MetaController(
        ICriarMetaUseCase criarMetaUseCase,
        IEditarMetaUseCase editarMetaUseCase,
        IExcluirMetaUseCase excluirMetaUseCase,
        IListarMetaUseCase listarMetaUseCase,
        IVincularTransacaoMetaUseCase vincularTransacaoMetaUseCase,
        IDesvincularTransacaoMetaUseCase desvincularTransacaoMetaUseCase)
    {
        _criarMetaUseCase = criarMetaUseCase ?? throw new ArgumentNullException(nameof(criarMetaUseCase));
        _editarMetaUseCase = editarMetaUseCase ?? throw new ArgumentNullException(nameof(editarMetaUseCase));
        _excluirMetaUseCase = excluirMetaUseCase ?? throw new ArgumentNullException(nameof(excluirMetaUseCase));
        _listarMetaUseCase = listarMetaUseCase ?? throw new ArgumentNullException(nameof(listarMetaUseCase));
        _vincularTransacaoMetaUseCase = vincularTransacaoMetaUseCase ?? throw new ArgumentNullException(nameof(vincularTransacaoMetaUseCase));
        _desvincularTransacaoMetaUseCase = desvincularTransacaoMetaUseCase ?? throw new ArgumentNullException(nameof(desvincularTransacaoMetaUseCase));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MetaResponse>>> Listar(
        CancellationToken ct = default)
    {
        var request = new ListarMetaRequest { MembroId = ObterMembroIdAtual() };
        var response = await _listarMetaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<MetaResponse>> Criar(
        [FromBody] CriarMetaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _criarMetaUseCase.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(Criar), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MetaResponse>> Editar(
        int id,
        [FromBody] EditarMetaRequest request,
        CancellationToken ct = default)
    {
        request.Id = id;
        request.MembroId = ObterMembroIdAtual();
        var response = await _editarMetaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<MetaResponse>> Excluir(
        int id,
        CancellationToken ct = default)
    {
        var response = await _excluirMetaUseCase.ExecutarAsync(
            new ExcluirMetaRequest { Id = id, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }

    /// <summary>Vincula uma transação existente a uma meta.</summary>
    [HttpPost("vincular-transacao")]
    public async Task<ActionResult<MetaResponse>> VincularTransacao(
        [FromBody] VincularTransacaoMetaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _vincularTransacaoMetaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Desvincula uma transação de uma meta.</summary>
    [HttpPost("desvincular-transacao")]
    public async Task<ActionResult<MetaResponse>> DesvincularTransacao(
        [FromBody] DesvincularTransacaoMetaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _desvincularTransacaoMetaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }
}
