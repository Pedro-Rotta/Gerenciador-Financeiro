using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using Microsoft.Extensions.Logging;

namespace GerenciadorContaFamiliar.Infrastructure.Services;

/// <summary>
/// Só no desenvolvimento, sem SMTP configurado: o e-mail (com o código) vai para o console da API.
/// </summary>
public class ConsoleEnvioDeEmail : IEnvioDeEmail
{
    private readonly ILogger<ConsoleEnvioDeEmail> _logger;

    public ConsoleEnvioDeEmail(ILogger<ConsoleEnvioDeEmail> logger)
    {
        _logger = logger;
    }

    public Task EnviarAsync(string para, string assunto, string texto, CancellationToken ct = default)
    {
        _logger.LogWarning("E-mail NÃO enviado (desenvolvimento, sem SMTP) para {Para} — {Assunto}: {Texto}", para, assunto, texto);
        return Task.CompletedTask;
    }
}
