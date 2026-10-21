using System.Security.Claims;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Services;

/// <summary>
/// Lê o membro logado a partir do token JWT da requisição HTTP atual.
/// Usado pela auditoria automática do DbContext para registrar quem fez cada alteração.
/// </summary>
public class UsuarioAtualHttp : IUsuarioAtual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioAtualHttp(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public int? MembroId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            return claim is not null && int.TryParse(claim.Value, out var id) && id > 0 ? id : null;
        }
    }
}
