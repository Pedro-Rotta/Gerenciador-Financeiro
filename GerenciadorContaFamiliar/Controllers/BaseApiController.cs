using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

/// <summary>
/// Base controller com helper methods para extrair dados do usuário autenticado (JWT).
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Extrai o MembroId do claim NameIdentifier do token JWT.
    /// Lança UnauthorizedException se o claim não estiver presente.
    /// </summary>
    protected int ObterMembroIdAtual()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Token JWT não contém o identificador do membro.");

        if (!int.TryParse(claim.Value, out var membroId))
            throw new UnauthorizedAccessException("Identificador do membro inválido no token.");

        return membroId;
    }

    /// <summary>
    /// Tenta extrair o MembroId sem lançar exceção.
    /// </summary>
    protected bool TentarObterMembroIdAtual(out int membroId)
    {
        membroId = 0;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out membroId);
    }
}
