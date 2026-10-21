using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace GerenciadorContaFamiliar.Infrastructure.Services;

/// <summary>
/// Envio por SMTP (MailKit). Configuração no User Secrets / variáveis de ambiente:
/// Email:Host, Email:Porta (padrão 587), Email:Usuario, Email:Senha e Email:Remetente
/// (padrão: o próprio usuário). TLS sempre: porta 465 cifra na conexão; as outras exigem STARTTLS.
/// </summary>
public class SmtpEnvioDeEmail : IEnvioDeEmail
{
    // Sem resposta do servidor em 15 s, desiste (a tela não fica presa em "Enviando...")
    private const int TempoLimiteMs = 15_000;

    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEnvioDeEmail> _logger;

    public SmtpEnvioDeEmail(IConfiguration configuration, ILogger<SmtpEnvioDeEmail> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnviarAsync(string para, string assunto, string texto, CancellationToken ct = default)
    {
        // Tudo dentro do try: até erro de configuração (ex.: remetente inválido) fica no log
        try
        {
            var host = _configuration["Email:Host"]
                ?? throw new InvalidOperationException("Email:Host não configurado.");
            var porta = int.TryParse(_configuration["Email:Porta"], out var p) ? p : 587;
            var usuario = _configuration["Email:Usuario"];
            var remetente = _configuration["Email:Remetente"] ?? usuario
                ?? throw new InvalidOperationException("Email:Remetente não configurado.");

            var mensagem = new MimeMessage();
            mensagem.From.Add(MailboxAddress.Parse(remetente));
            mensagem.To.Add(MailboxAddress.Parse(para));
            mensagem.Subject = assunto;
            mensagem.Body = new TextPart("plain") { Text = texto };

            using var cliente = new SmtpClient { Timeout = TempoLimiteMs };
            var tls = porta == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
            await cliente.ConnectAsync(host, porta, tls, ct);
            if (!string.IsNullOrWhiteSpace(usuario))
                await cliente.AuthenticateAsync(usuario, _configuration["Email:Senha"] ?? string.Empty, ct);
            await cliente.SendAsync(mensagem, ct);
            await cliente.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Para}", para);
            throw;
        }
    }
}
