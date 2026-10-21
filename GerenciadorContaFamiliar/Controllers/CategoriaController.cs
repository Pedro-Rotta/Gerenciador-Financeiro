using GerenciadorContaFamiliar.Application.DTOs.Requests.Categorias;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Categorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class CategoriaController : BaseApiController
{
    private readonly ICriarCategoriaUseCase _criarCategoriaUseCase;
    private readonly IEditarCategoriaUseCase _editarCategoriaUseCase;
    private readonly IExcluirCategoriaUseCase _excluirCategoriaUseCase;
    private readonly IListarCategoriaUseCase _listarCategoriaUseCase;

    public CategoriaController(
        ICriarCategoriaUseCase criarCategoriaUseCase,
        IEditarCategoriaUseCase editarCategoriaUseCase,
        IExcluirCategoriaUseCase excluirCategoriaUseCase,
        IListarCategoriaUseCase listarCategoriaUseCase
        )
    {
        _criarCategoriaUseCase = criarCategoriaUseCase ?? throw new ArgumentNullException(nameof(criarCategoriaUseCase));
        _editarCategoriaUseCase = editarCategoriaUseCase ?? throw new ArgumentNullException(nameof(editarCategoriaUseCase));
        _excluirCategoriaUseCase = excluirCategoriaUseCase ?? throw new ArgumentNullException(nameof(excluirCategoriaUseCase));
        _listarCategoriaUseCase = listarCategoriaUseCase ?? throw new ArgumentNullException(nameof(listarCategoriaUseCase));
    }
    [HttpPost]
    public async Task<ActionResult<CategoriaResponse>> Criar(
        [FromBody] CriarCategoriaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _criarCategoriaUseCase.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(Criar), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoriaResponse>> Editar(
        int id,
        [FromBody] EditarCategoriaRequest request,
        CancellationToken ct = default)
    {
        request.Id = id;
        request.MembroId = ObterMembroIdAtual();
        var response = await _editarCategoriaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<CategoriaResponse>> Excluir(
        int id,
        CancellationToken ct = default)
    {
        var membroId = ObterMembroIdAtual();
        var response = await _excluirCategoriaUseCase.ExecutarAsync(
            new ExcluirCategoriaRequest { Id = id, MembroId = membroId }, ct);
        return Ok(response);
    }
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoriaResponse>>> Listar(
        [FromQuery] ListarCategoriaRequest request, 
        CancellationToken ct = default)
    {
        // O controller SETA o MembroId do JWT (igual a todos os seus outros endpoints)
        request.MembroId = ObterMembroIdAtual();
        
        var response = await _listarCategoriaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }
}
