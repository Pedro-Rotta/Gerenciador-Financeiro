using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class ConviteController : BaseApiController
{
    private readonly ICriarConviteUseCase _criarConviteUseCase;
    private readonly IListarConvitesRecebidosUseCase _listarConvitesRecebidosUseCase;
    private readonly IListarConvitesDaContaUseCase _listarConvitesDaContaUseCase;
    private readonly IAceitarConviteUseCase _aceitarConviteUseCase;
    private readonly IRecusarConviteUseCase _recusarConviteUseCase;
    private readonly ICancelarConviteUseCase _cancelarConviteUseCase;

    public ConviteController(
        ICriarConviteUseCase criarConviteUseCase,
        IListarConvitesRecebidosUseCase listarConvitesRecebidosUseCase,
        IListarConvitesDaContaUseCase listarConvitesDaContaUseCase,
        IAceitarConviteUseCase aceitarConviteUseCase,
        IRecusarConviteUseCase recusarConviteUseCase,
        ICancelarConviteUseCase cancelarConviteUseCase)
    {
        _criarConviteUseCase = criarConviteUseCase ?? throw new ArgumentNullException(nameof(criarConviteUseCase));
        _listarConvitesRecebidosUseCase = listarConvitesRecebidosUseCase ?? throw new ArgumentNullException(nameof(listarConvitesRecebidosUseCase));
        _listarConvitesDaContaUseCase = listarConvitesDaContaUseCase ?? throw new ArgumentNullException(nameof(listarConvitesDaContaUseCase));
        _aceitarConviteUseCase = aceitarConviteUseCase ?? throw new ArgumentNullException(nameof(aceitarConviteUseCase));
        _recusarConviteUseCase = recusarConviteUseCase ?? throw new ArgumentNullException(nameof(recusarConviteUseCase));
        _cancelarConviteUseCase = cancelarConviteUseCase ?? throw new ArgumentNullException(nameof(cancelarConviteUseCase));
    }

    /// <summary>Convida um membro (por nome de usuário ou e-mail) para uma conta conjunta. Só o criador.</summary>
    [HttpPost]
    public async Task<ActionResult<ConviteResponse>> Criar(
        [FromBody] CriarConviteRequest request,
        CancellationToken ct = default)
    {
        // Quem convida vem do token JWT, nunca do cliente.
        request.MembroId = ObterMembroIdAtual();
        var response = await _criarConviteUseCase.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(Criar), new { id = response.Id }, response);
    }

    /// <summary>Convites pendentes recebidos pelo membro logado (sino de notificações).</summary>
    [HttpGet("recebidos")]
    public async Task<ActionResult<IEnumerable<ConviteResponse>>> ListarRecebidos(
        CancellationToken ct = default)
    {
        var request = new ListarConvitesRecebidosRequest { MembroId = ObterMembroIdAtual() };
        var response = await _listarConvitesRecebidosUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Convites pendentes de uma conta conjunta (?contaConjuntaId=X). Só o criador.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConviteResponse>>> ListarDaConta(
        [FromQuery] ListarConvitesDaContaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _listarConvitesDaContaUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>O convidado aceita o convite e passa a participar da conta conjunta.</summary>
    [HttpPost("{id:int}/aceitar")]
    public async Task<ActionResult<ConviteResponse>> Aceitar(
        int id,
        CancellationToken ct = default)
    {
        var response = await _aceitarConviteUseCase.ExecutarAsync(
            new AceitarConviteRequest { Id = id, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }

    /// <summary>O convidado recusa o convite.</summary>
    [HttpPost("{id:int}/recusar")]
    public async Task<ActionResult<ConviteResponse>> Recusar(
        int id,
        CancellationToken ct = default)
    {
        var response = await _recusarConviteUseCase.ExecutarAsync(
            new RecusarConviteRequest { Id = id, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }

    /// <summary>O criador cancela um convite pendente.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ConviteResponse>> Cancelar(
        int id,
        CancellationToken ct = default)
    {
        var response = await _cancelarConviteUseCase.ExecutarAsync(
            new CancelarConviteRequest { Id = id, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }
}
