using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class NotificacaoController : BaseApiController
{
    private readonly ICriarNotificacaoUseCase _criarNotificacaoUseCase;
    private readonly IEditarNotificacaoUseCase _editarNotificacaoUseCase;
    private readonly IExcluirNotificacaoUseCase _excluirNotificacaoUseCase;
    private readonly IListarNotificacaoUseCase _listarNotificacaoUseCase;
    private readonly IObterPreferenciasNotificacaoUseCase _obterPreferenciasUseCase;
    private readonly ISalvarPreferenciasNotificacaoUseCase _salvarPreferenciasUseCase;

    public NotificacaoController(
        ICriarNotificacaoUseCase criarNotificacaoUseCase,
        IEditarNotificacaoUseCase editarNotificacaoUseCase,
        IExcluirNotificacaoUseCase excluirNotificacaoUseCase,
        IListarNotificacaoUseCase listarNotificacaoUseCase,
        IObterPreferenciasNotificacaoUseCase obterPreferenciasUseCase,
        ISalvarPreferenciasNotificacaoUseCase salvarPreferenciasUseCase)
    {
        _criarNotificacaoUseCase = criarNotificacaoUseCase ?? throw new ArgumentNullException(nameof(criarNotificacaoUseCase));
        _editarNotificacaoUseCase = editarNotificacaoUseCase ?? throw new ArgumentNullException(nameof(editarNotificacaoUseCase));
        _excluirNotificacaoUseCase = excluirNotificacaoUseCase ?? throw new ArgumentNullException(nameof(excluirNotificacaoUseCase));
        _listarNotificacaoUseCase = listarNotificacaoUseCase ?? throw new ArgumentNullException(nameof(listarNotificacaoUseCase));
        _obterPreferenciasUseCase = obterPreferenciasUseCase ?? throw new ArgumentNullException(nameof(obterPreferenciasUseCase));
        _salvarPreferenciasUseCase = salvarPreferenciasUseCase ?? throw new ArgumentNullException(nameof(salvarPreferenciasUseCase));
    }

    /// <summary>Lista as notificações do membro logado (mais recentes primeiro).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificacaoResponse>>> Listar(
        CancellationToken ct = default)
    {
        var request = new ListarNotificacaoRequest { MembroId = ObterMembroIdAtual() };
        var response = await _listarNotificacaoUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Preferências de notificação do membro (padrões se ele nunca salvou).</summary>
    [HttpGet("preferencias")]
    public async Task<ActionResult<PreferenciasNotificacaoResponse>> ObterPreferencias(CancellationToken ct = default)
    {
        var response = await _obterPreferenciasUseCase.ExecutarAsync(
            new ObterPreferenciasNotificacaoRequest { MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }

    /// <summary>Salva todas as preferências (corpo completo); inválido → 400.</summary>
    [HttpPut("preferencias")]
    public async Task<ActionResult<PreferenciasNotificacaoResponse>> SalvarPreferencias(
        [FromBody] SalvarPreferenciasNotificacaoRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _salvarPreferenciasUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<NotificacaoResponse>> Criar(
        [FromBody] CriarNotificacaoRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _criarNotificacaoUseCase.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(Criar), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<NotificacaoResponse>> Editar(
        int id,
        [FromBody] EditarNotificacaoRequest request,
        CancellationToken ct = default)
    {
        request.Id = id;
        request.MembroId = ObterMembroIdAtual();
        var response = await _editarNotificacaoUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<NotificacaoResponse>> Excluir(
        int id,
        CancellationToken ct = default)
    {
        var response = await _excluirNotificacaoUseCase.ExecutarAsync(
            new ExcluirNotificacaoRequest { Id = id, MembroId = ObterMembroIdAtual() }, ct);
        return Ok(response);
    }
}
