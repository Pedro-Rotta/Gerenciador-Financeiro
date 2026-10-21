using System.Net;
using System.Text.Json;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Middleware;

/// <summary>
/// Middleware global de tratamento de exceções.
/// Converte exceções não tratadas em respostas HTTP JSON padronizadas,
/// evitando que stack traces vazem para o cliente (risco de segurança).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            await TratarAsync(context, ex, HttpStatusCode.Unauthorized,
                "Não autorizado.", LogLevel.Warning);
        }
        catch (AcessoNegadoException ex)
        {
            // Autenticado, mas sem permissão sobre o recurso (conta/transação de outro membro)
            await TratarAsync(context, ex, HttpStatusCode.Forbidden,
                ex.Message, LogLevel.Warning);
        }
        catch (ArgumentException ex)
        {
            // Cobre ArgumentException e ArgumentNullException (validação de dados/negócio)
            await TratarAsync(context, ex, HttpStatusCode.BadRequest,
                ex.Message, LogLevel.Warning);
        }
        catch (KeyNotFoundException ex)
        {
            // Registro não encontrado (ex.: "Convite não encontrado.")
            await TratarAsync(context, ex, HttpStatusCode.NotFound,
                ex.Message, LogLevel.Warning);
        }
        catch (InvalidOperationException ex)
        {
            // Regra de negócio violada (estado inválido, já inativo, duplicidade...)
            await TratarAsync(context, ex, HttpStatusCode.Conflict,
                ex.Message, LogLevel.Warning);
        }
        catch (Exception ex)
        {
            await TratarAsync(context, ex, HttpStatusCode.InternalServerError,
                "Ocorreu um erro interno no servidor.", LogLevel.Error);
        }
    }

    private async Task TratarAsync(
        HttpContext context,
        Exception ex,
        HttpStatusCode statusCode,
        string mensagem,
        LogLevel logLevel)
    {
        _logger.Log(logLevel, ex, "{Mensagem}", mensagem);

        // Se a resposta já começou a ser enviada, não é possível formatar o erro.
        if (context.Response.HasStarted)
        {
            _logger.LogError("A resposta já foi iniciada; a exceção não pôde ser formatada.");
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new ErroApiResponse(
            Status: (int)statusCode,
            Mensagem: mensagem,
            TraceId: context.TraceIdentifier,
            Detalhes: _env.IsDevelopment() ? ex.ToString() : null);

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

/// <summary>Corpo padronizado de resposta de erro da API.</summary>
public record ErroApiResponse(
    int Status,
    string Mensagem,
    string TraceId,
    string? Detalhes
);