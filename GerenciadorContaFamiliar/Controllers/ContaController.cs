using GerenciadorContaFamiliar.Application.DTOs.Requests.Contas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Contas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ContaController : BaseApiController
{
    private readonly ICriarContaUseCase _criarContaUseCase;
    private readonly IEditarContaUseCase _editarContaUseCase;
    private readonly IExcluirContaUseCase _excluirContaUseCase;
    private readonly IListarContaUseCase _listarContaUseCase;
    private readonly IAlterarContaPrincipalUseCase _alterarContaPrincipalUseCase;
    private readonly IDefinirSaldoInicialUseCase _definirSaldoInicialUseCase;

    public ContaController(
        ICriarContaUseCase criarContaUseCase,
        IEditarContaUseCase editarContaUseCase,
        IExcluirContaUseCase excluirContaUseCase,
        IListarContaUseCase listarContaUseCase,
        IAlterarContaPrincipalUseCase alterarContaPrincipalUseCase,
        IDefinirSaldoInicialUseCase definirSaldoInicialUseCase)
    {
        _definirSaldoInicialUseCase = definirSaldoInicialUseCase ?? throw new ArgumentNullException(nameof(definirSaldoInicialUseCase));
        _criarContaUseCase = criarContaUseCase ?? throw new ArgumentNullException(nameof(criarContaUseCase));
        _editarContaUseCase = editarContaUseCase ?? throw new ArgumentNullException(nameof(editarContaUseCase));
        _excluirContaUseCase = excluirContaUseCase ?? throw new ArgumentNullException(nameof(excluirContaUseCase));
        _listarContaUseCase = listarContaUseCase ?? throw new ArgumentNullException(nameof(listarContaUseCase));
        _alterarContaPrincipalUseCase = alterarContaPrincipalUseCase ?? throw new ArgumentNullException(nameof(alterarContaPrincipalUseCase));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContaResponse>>> Listar(
        CancellationToken ct = default)
    {
        var membroId = ObterMembroIdAtual();
        var request = new ListarContaRequest { MembroId = membroId };
        var response = await _listarContaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }
    [HttpPost]
    public async Task<ActionResult<ContaResponse>> Criar(
        [FromBody] CriarContaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _criarContaUseCase.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(Criar), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContaResponse>> Editar(
        int id,
        [FromBody] EditarContaRequest request,
        CancellationToken ct = default)
    {
        request.Id = id;
        request.MembroId = ObterMembroIdAtual();
        var response = await _editarContaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ContaResponse>> Excluir(
        int id,
        CancellationToken ct = default)
    {
        var response = await _excluirContaUseCase.ExecutarAsync(
            new ExcluirContaRequest { Id = id, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }

    [HttpPut("{id:int}/principal")]
    public async Task<ActionResult<ContaResponse>> AlterarPrincipal(
        int id,
        CancellationToken ct = default)
    {
        var response = await _alterarContaPrincipalUseCase.ExecutarAsync(
            new AlterarContaPrincipalRequest { Id = id, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }

    /// <summary>Saldo inicial da conta (uma vez só): libera os lançamentos.</summary>
    [HttpPost("{id:int}/saldo-inicial")]
    public async Task<ActionResult<ContaResponse>> DefinirSaldoInicial(
        int id,
        [FromBody] DefinirSaldoInicialRequest request,
        CancellationToken ct = default)
    {
        request.Id = id;
        request.MembroId = ObterMembroIdAtual();
        var response = await _definirSaldoInicialUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }
}
