using GerenciadorContaFamiliar.Application.DTOs.Requests.Verificacao;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Verificacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

/// <summary>
/// Verificação ao abrir o app: o front chama ao carregar e junto da atualização do sino;
/// o servidor roda no máximo uma vez por hora para cada membro.
/// </summary>
[Authorize]
[Route("api/[controller]")]
public class VerificacaoController : BaseApiController
{
    private readonly IExecutarVerificacaoUseCase _executarVerificacaoUseCase;
    private readonly ILogger<VerificacaoController> _logger;

    public VerificacaoController(
        IExecutarVerificacaoUseCase executarVerificacaoUseCase,
        ILogger<VerificacaoController> logger)
    {
        _executarVerificacaoUseCase = executarVerificacaoUseCase ?? throw new ArgumentNullException(nameof(executarVerificacaoUseCase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> Executar(CancellationToken ct = default)
    {
        var membroId = ObterMembroIdAtual();
        var response = await _executarVerificacaoUseCase.ExecutarAsync(
            new ExecutarVerificacaoRequest { MembroId = membroId }, ct);

        // Falha num passo não bloqueia nada: só fica registrada no log
        foreach (var falha in response.Falhas)
            _logger.LogWarning("Verificação do membro {MembroId} falhou no passo {Falha}", membroId, falha);

        return NoContent();
    }
}
