using GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Auth;

/// <summary>Troca a senha com o código recebido e derruba as sessões abertas.</summary>
public interface IRedefinirSenhaUseCase
{
    Task<MensagemResponse> ExecutarAsync(RedefinirSenhaRequest request, CancellationToken ct = default);
}
