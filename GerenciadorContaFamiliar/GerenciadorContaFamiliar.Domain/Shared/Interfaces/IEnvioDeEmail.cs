namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;

/// <summary>Envio de e-mail (SMTP na Infrastructure; no desenvolvimento sem SMTP, o console).</summary>
public interface IEnvioDeEmail
{
    Task EnviarAsync(string para, string assunto, string texto, CancellationToken ct = default);
}
