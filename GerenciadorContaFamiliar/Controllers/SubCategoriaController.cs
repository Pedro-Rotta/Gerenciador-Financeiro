using GerenciadorContaFamiliar.Application.DTOs.Requests.SubCategorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.SubCategorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class SubCategoriaController : BaseApiController
{
    private readonly ICriarSubCategoriaUseCase _criarSubCategoriaUseCase;
    private readonly IEditarSubCategoriaUseCase _editarSubCategoriaUseCase;
    private readonly IExcluirSubCategoriaUseCase _excluirSubCategoriaUseCase;
    private readonly IVincularSubCategoriaUseCase _vincularSubCategoriaUseCase;
    private readonly IDesvincularSubCategoriaUseCase _desvincularSubCategoriaUseCase;
    private readonly IListarSubCategoriaUseCase _listarSubCategoriaUseCase;

    public SubCategoriaController(
        ICriarSubCategoriaUseCase criarSubCategoriaUseCase,
        IEditarSubCategoriaUseCase editarSubCategoriaUseCase,
        IExcluirSubCategoriaUseCase excluirSubCategoriaUseCase,
        IVincularSubCategoriaUseCase vincularSubCategoriaUseCase,
        IDesvincularSubCategoriaUseCase desvincularSubCategoriaUseCase,
        IListarSubCategoriaUseCase listarSubCategoriaUseCase)
    {
        _criarSubCategoriaUseCase = criarSubCategoriaUseCase ?? throw new ArgumentNullException(nameof(criarSubCategoriaUseCase));
        _editarSubCategoriaUseCase = editarSubCategoriaUseCase ?? throw new ArgumentNullException(nameof(editarSubCategoriaUseCase));
        _excluirSubCategoriaUseCase = excluirSubCategoriaUseCase ?? throw new ArgumentNullException(nameof(excluirSubCategoriaUseCase));
        _vincularSubCategoriaUseCase = vincularSubCategoriaUseCase ?? throw new ArgumentNullException(nameof(vincularSubCategoriaUseCase));
        _desvincularSubCategoriaUseCase = desvincularSubCategoriaUseCase ?? throw new ArgumentNullException(nameof(desvincularSubCategoriaUseCase));
        _listarSubCategoriaUseCase = listarSubCategoriaUseCase ?? throw new ArgumentNullException(nameof(listarSubCategoriaUseCase));
    }
    [HttpPost]
    public async Task<ActionResult<SubCategoriaResponse>> Criar(
        [FromBody] CriarSubCategoriaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _criarSubCategoriaUseCase.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(Criar), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SubCategoriaResponse>> Editar(
        int id,
        [FromBody] EditarSubCategoriaRequest request,
        CancellationToken ct = default)
    {
        request.Id = id;
        request.MembroId = ObterMembroIdAtual();
        var response = await _editarSubCategoriaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<SubCategoriaResponse>> Excluir(
        int id,
        CancellationToken ct = default)
    {
        var membroId = ObterMembroIdAtual();
        var response = await _excluirSubCategoriaUseCase.ExecutarAsync(
            new ExcluirSubCategoriaRequest { Id = id, MembroId = membroId }, ct);
        return Ok(response);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubCategoriaResponse>>> Listar(
        [FromQuery] ListarSubCategoriaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _listarSubCategoriaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Vincula uma subcategoria existente a uma categoria (N:N).</summary>
    [HttpPost("vincular")]
    public async Task<ActionResult<CategoriaResponse>> Vincular(
        [FromBody] VincularSubCategoriaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _vincularSubCategoriaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Desvincula uma subcategoria de uma categoria (N:N).</summary>
    [HttpPost("desvincular")]
    public async Task<ActionResult<CategoriaResponse>> Desvincular(
        [FromBody] DesvincularSubCategoriaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _desvincularSubCategoriaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }
}
