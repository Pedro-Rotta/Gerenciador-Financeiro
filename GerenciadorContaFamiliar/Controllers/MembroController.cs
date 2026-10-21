using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;
using GerenciadorContaFamiliar.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class MembroController : BaseApiController
{
    private readonly ICriarMembroUseCase _criarMembroUseCase;
    private readonly IEditarMembroUseCase _editarMembroUseCase;
    private readonly IExcluirMembroUseCase _excluirMembroUseCase;
    private readonly IAlterarSenhaUseCase _alterarSenhaUseCase;
    private readonly IObterPerfilUseCase _obterPerfilUseCase;
    private readonly IEncerrarOutrasSessoesUseCase _encerrarOutrasSessoesUseCase;
    private readonly IObterPreferenciasUsoUseCase _obterPreferenciasUsoUseCase;
    private readonly ISalvarPreferenciasUsoUseCase _salvarPreferenciasUsoUseCase;

    public MembroController(
        IObterPreferenciasUsoUseCase obterPreferenciasUsoUseCase,
        ISalvarPreferenciasUsoUseCase salvarPreferenciasUsoUseCase,
        ICriarMembroUseCase criarMembroUseCase,
        IEditarMembroUseCase editarMembroUseCase,
        IExcluirMembroUseCase excluirMembroUseCase,
        IAlterarSenhaUseCase alterarSenhaUseCase,
        IObterPerfilUseCase obterPerfilUseCase,
        IEncerrarOutrasSessoesUseCase encerrarOutrasSessoesUseCase)
    {
        _obterPerfilUseCase = obterPerfilUseCase ?? throw new ArgumentNullException(nameof(obterPerfilUseCase));
        _obterPreferenciasUsoUseCase = obterPreferenciasUsoUseCase ?? throw new ArgumentNullException(nameof(obterPreferenciasUsoUseCase));
        _salvarPreferenciasUsoUseCase = salvarPreferenciasUsoUseCase ?? throw new ArgumentNullException(nameof(salvarPreferenciasUsoUseCase));
        _encerrarOutrasSessoesUseCase = encerrarOutrasSessoesUseCase ?? throw new ArgumentNullException(nameof(encerrarOutrasSessoesUseCase));
        _alterarSenhaUseCase = alterarSenhaUseCase ?? throw new ArgumentNullException(nameof(alterarSenhaUseCase));
        _criarMembroUseCase = criarMembroUseCase ?? throw new ArgumentNullException(nameof(criarMembroUseCase));
        _editarMembroUseCase = editarMembroUseCase ?? throw new ArgumentNullException(nameof(editarMembroUseCase));
        _excluirMembroUseCase = excluirMembroUseCase ?? throw new ArgumentNullException(nameof(excluirMembroUseCase));
    }
    
    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting(AuthController.PoliticaCadastro)]  // 5 cadastros por hora por IP
    public async Task<ActionResult<MembroResponse>> Criar(
        [FromBody] CriarMembroRequest request,
        CancellationToken ct = default)
    {
        var response = await _criarMembroUseCase.ExecutarAsync(request, ct);
        return CreatedAtAction(nameof(Criar), new { id = response.Id }, response);
    }

    /// <summary>Edita o membro autenticado. O Id é sempre extraído do JWT.</summary>
    [HttpPut]
    public async Task<ActionResult<MembroResponse>> Editar(
        [FromBody] EditarMembroRequest request,
        CancellationToken ct = default)
    {
        request.Id = ObterMembroIdAtual();
        var response = await _editarMembroUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Exclui (inativa) o membro autenticado. O Id é sempre extraído do JWT.</summary>
    [HttpDelete]
    public async Task<ActionResult<MembroResponse>> Excluir(
        [FromBody] ExcluirMembroRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var response = await _excluirMembroUseCase.ExecutarAsync(request, ct);
        Response.Cookies.Delete(CookieDeSessao.Nome, CookieDeSessao.Opcoes(DateTime.UtcNow));
        return Ok(response);
    }

    /// <summary>Perfil do membro logado (aba Conta das Configurações).</summary>
    [HttpGet]
    public async Task<ActionResult<MembroResponse>> ObterPerfil(CancellationToken ct = default)
    {
        return Ok(await _obterPerfilUseCase.ExecutarAsync(ObterMembroIdAtual(), ct));
    }

    /// <summary>Preferências de uso do app (Configurações → Preferências).</summary>
    [HttpGet("preferencias")]
    public async Task<ActionResult<PreferenciasUsoResponse>> ObterPreferencias(CancellationToken ct = default)
    {
        return Ok(await _obterPreferenciasUsoUseCase.ExecutarAsync(ObterMembroIdAtual(), ct));
    }

    [HttpPut("preferencias")]
    public async Task<ActionResult<PreferenciasUsoResponse>> SalvarPreferencias(
        [FromBody] SalvarPreferenciasUsoRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        return Ok(await _salvarPreferenciasUsoUseCase.ExecutarAsync(request, ct));
    }

    /// <summary>
    /// Encerra as outras sessões: todos os tokens caem e este navegador recebe um cookie novo.
    /// </summary>
    [HttpPost("encerrar-sessoes")]
    public async Task<ActionResult<LoginResponse>> EncerrarOutrasSessoes(CancellationToken ct = default)
    {
        var sessao = await _encerrarOutrasSessoesUseCase.ExecutarAsync(ObterMembroIdAtual(), ct);
        Response.Cookies.Append(CookieDeSessao.Nome, sessao.Token, CookieDeSessao.Opcoes(sessao.ExpiraEm));
        return Ok(sessao);
    }

    /// <summary>
    /// Altera a senha do membro logado (com a senha atual). As outras sessões caem; este
    /// navegador recebe um cookie novo e continua logado.
    /// </summary>
    [HttpPut("senha")]
    [EnableRateLimiting(AuthController.PoliticaRecuperacao)]
    public async Task<ActionResult<LoginResponse>> AlterarSenha(
        [FromBody] AlterarSenhaRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var sessao = await _alterarSenhaUseCase.ExecutarAsync(request, ct);
        Response.Cookies.Append(CookieDeSessao.Nome, sessao.Token, CookieDeSessao.Opcoes(sessao.ExpiraEm));
        // Devolve a sessão nova (sem o token, que vai só no cookie): o front atualiza a validade
        return Ok(sessao);
    }
}
