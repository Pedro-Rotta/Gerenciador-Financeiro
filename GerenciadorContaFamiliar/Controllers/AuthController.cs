using GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Auth;
using GerenciadorContaFamiliar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GerenciadorContaFamiliar.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    /// <summary>Política de limite de tentativas do login (registrada no Program.cs).</summary>
    public const string PoliticaLogin = "login";

    /// <summary>Limite de "esqueceu a senha" e "alterar senha": 5 pedidos a cada 10 minutos por IP.</summary>
    public const string PoliticaRecuperacao = "recuperacao";

    /// <summary>Limite do cadastro (POST /api/Membro): 5 contas por hora por IP.</summary>
    public const string PoliticaCadastro = "cadastro";

    private readonly ILoginUseCase _loginUseCase;
    private readonly IEsqueciSenhaUseCase _esqueciSenhaUseCase;
    private readonly IRedefinirSenhaUseCase _redefinirSenhaUseCase;

    public AuthController(
        ILoginUseCase loginUseCase,
        IEsqueciSenhaUseCase esqueciSenhaUseCase,
        IRedefinirSenhaUseCase redefinirSenhaUseCase)
    {
        _loginUseCase = loginUseCase ?? throw new ArgumentNullException(nameof(loginUseCase));
        _esqueciSenhaUseCase = esqueciSenhaUseCase ?? throw new ArgumentNullException(nameof(esqueciSenhaUseCase));
        _redefinirSenhaUseCase = redefinirSenhaUseCase ?? throw new ArgumentNullException(nameof(redefinirSenhaUseCase));
    }

    /// <summary>
    /// "Esqueceu a senha": manda um código de 6 dígitos para o e-mail da conta. A resposta é a
    /// mesma exista ou não a conta (não revela quem tem cadastro).
    /// </summary>
    [HttpPost("esqueci-senha")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticaRecuperacao)]
    public async Task<ActionResult<MensagemResponse>> EsqueciSenha(
        [FromBody] EsqueciSenhaRequest request,
        CancellationToken ct = default)
    {
        return Ok(await _esqueciSenhaUseCase.ExecutarAsync(request, ct));
    }

    /// <summary>
    /// Troca a senha com o código recebido; as sessões abertas caem e a pessoa entra de novo.
    /// </summary>
    [HttpPost("redefinir-senha")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticaRecuperacao)]
    public async Task<ActionResult<MensagemResponse>> RedefinirSenha(
        [FromBody] RedefinirSenhaRequest request,
        CancellationToken ct = default)
    {
        return Ok(await _redefinirSenhaUseCase.ExecutarAsync(request, ct));
    }

    /// <summary>
    /// Realiza login. Grava o token JWT no cookie de sessão (HttpOnly).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]  // Login NÃO precisa de token (é o endpoint que GERA o token)
    [EnableRateLimiting(PoliticaLogin)]  // 5 tentativas por minuto por IP
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct = default)
    {
        var response = await _loginUseCase.ExecutarAsync(request, ct);
        Response.Cookies.Append(CookieDeSessao.Nome, response.Token, CookieDeSessao.Opcoes(response.ExpiraEm));
        return Ok(response);
    }

    /// <summary>
    /// Encerra a sessão apagando o cookie. Anônimo: funciona mesmo com a sessão já expirada.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieDeSessao.Nome, CookieDeSessao.Opcoes(DateTime.UtcNow));
        return NoContent();
    }
}
