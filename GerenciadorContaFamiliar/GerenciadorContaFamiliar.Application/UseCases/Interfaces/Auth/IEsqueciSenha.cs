using GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Auth;

/// <summary>Gera e manda por e-mail o código do "Esqueceu a senha" (resposta igual exista ou não a conta).</summary>
public interface IEsqueciSenhaUseCase
{
    Task<MensagemResponse> ExecutarAsync(EsqueciSenhaRequest request, CancellationToken ct = default);
}
