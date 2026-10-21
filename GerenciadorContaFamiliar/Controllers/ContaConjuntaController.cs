using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ContaConjuntaController : BaseApiController
{
    private readonly ICriarContaConjuntaUseCase _criarContaConjuntaUseCase;
    private readonly IEditarContaConjuntaUseCase _editarContaConjuntaUseCase;
    private readonly IExcluirContaConjuntaUseCase _excluirContaConjuntaUseCase;
    private readonly IListarContaConjuntaUseCase _listarContaConjuntaUseCase;
    private readonly IRemoverParticipanteUseCase _removerParticipanteUseCase;
    private readonly ISairContaConjuntaUseCase _sairContaConjuntaUseCase;

    public ContaConjuntaController(
        ICriarContaConjuntaUseCase criarContaConjuntaUseCase,
        IEditarContaConjuntaUseCase editarContaConjuntaUseCase,
        IExcluirContaConjuntaUseCase excluirContaConjuntaUseCase,
        IListarContaConjuntaUseCase listarContaConjuntaUseCase,
        IRemoverParticipanteUseCase removerParticipanteUseCase,
        ISairContaConjuntaUseCase sairContaConjuntaUseCase)
    {
        _criarContaConjuntaUseCase = criarContaConjuntaUseCase ?? throw new ArgumentNullException(nameof(criarContaConjuntaUseCase));
        _editarContaConjuntaUseCase = editarContaConjuntaUseCase ?? throw new ArgumentNullException(nameof(editarContaConjuntaUseCase));
        _excluirContaConjuntaUseCase = excluirContaConjuntaUseCase ?? throw new ArgumentNullException(nameof(excluirContaConjuntaUseCase));
        _listarContaConjuntaUseCase = listarContaConjuntaUseCase ?? throw new ArgumentNullException(nameof(listarContaConjuntaUseCase));
        _removerParticipanteUseCase = removerParticipanteUseCase ?? throw new ArgumentNullException(nameof(removerParticipanteUseCase));
        _sairContaConjuntaUseCase = sairContaConjuntaUseCase ?? throw new ArgumentNullException(nameof(sairContaConjuntaUseCase));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContaConjuntaResponse>>> Listar(
        CancellationToken ct = default)
    {
        var membroId = ObterMembroIdAtual();
        var request = new ListarContaConjuntaRequest { MembroId = membroId };
        var response = await _listarContaConjuntaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }
    [HttpPost]
    public async Task<ActionResult<ContaConjuntaResponse>> Criar(
        [FromBody] CriarContaConjuntaRequest request,
        CancellationToken ct = default)
    {
        // O criador vem do token JWT, nunca confia no cliente.
        request.CriadorId = ObterMembroIdAtual();
        var response = await _criarContaConjuntaUseCase.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(Criar), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ContaConjuntaResponse>> Editar(
        int id,
        [FromBody] EditarContaConjuntaRequest request,
        CancellationToken ct = default)
    {
        request.Id = id;
        request.MembroId = ObterMembroIdAtual();
        var response = await _editarContaConjuntaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ContaConjuntaResponse>> Excluir(
        int id,
        CancellationToken ct = default)
    {
        var membroId = ObterMembroIdAtual();
        var response = await _excluirContaConjuntaUseCase.ExecutarAsync(
            new ExcluirContaConjuntaRequest { Id = id, MembroId = membroId }, ct);
        return Ok(response);
    }

    /// <summary>O criador remove um participante da conta conjunta.</summary>
    [HttpDelete("{id:int}/participantes/{participanteId:int}")]
    public async Task<ActionResult<ContaConjuntaResponse>> RemoverParticipante(
        int id,
        int participanteId,
        CancellationToken ct = default)
    {
        var response = await _removerParticipanteUseCase.ExecutarAsync(
            new RemoverParticipanteRequest { Id = id, ParticipanteId = participanteId, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }

    /// <summary>O membro logado sai da conta conjunta (o criador não pode sair).</summary>
    [HttpPost("{id:int}/sair")]
    public async Task<ActionResult<ContaConjuntaResponse>> Sair(
        int id,
        CancellationToken ct = default)
    {
        var response = await _sairContaConjuntaUseCase.ExecutarAsync(
            new SairContaConjuntaRequest { Id = id, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }
}
