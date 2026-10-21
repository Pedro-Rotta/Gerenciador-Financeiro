using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

/// <summary>
/// Verificação de saúde: usada pelo despertador (cron-job.org) e pela Render a cada publicação.
/// Não consulta o banco — assim o Neon gratuito pode dormir e não gasta as horas do mês.
/// </summary>
[ApiController]
[Route("api/saude")]
[AllowAnonymous]
public class SaudeController : ControllerBase
{
    /// <summary>Chave em HttpContext.Items com o X-Forwarded-For como chegou (só no diagnóstico).</summary>
    public const string ChaveForwardedForBruto = "XForwardedForBruto";

    private readonly IConfiguration _configuracao;

    public SaudeController(IConfiguration configuracao)
    {
        _configuracao = configuracao ?? throw new ArgumentNullException(nameof(configuracao));
    }

    [HttpGet]
    public IActionResult Obter() => Ok(new { status = "ok" });

    /// <summary>
    /// Diagnóstico do proxy (só com ProxyReverso:Diagnostico=true; senão 404): mostra o X-Forwarded-For
    /// que chegou e o IP que a API usa, para acertar ProxyReverso:Saltos. Desligar depois de conferir.
    /// </summary>
    [HttpGet("ip")]
    public IActionResult Ip()
    {
        if (!_configuracao.GetValue<bool>("ProxyReverso:Diagnostico"))
            return NotFound();

        // O cabeçalho bruto é guardado no Program.cs antes do UseForwardedHeaders, que consome as
        // entradas usadas e deixa em X-Forwarded-For só as que sobraram (à esquerda)
        return Ok(new
        {
            ipVisto = HttpContext.Connection.RemoteIpAddress?.ToString(),
            xForwardedForRecebido = HttpContext.Items[ChaveForwardedForBruto] as string,
            xForwardedForNaoUsado = Request.Headers["X-Forwarded-For"].ToString()
        });
    }
}
