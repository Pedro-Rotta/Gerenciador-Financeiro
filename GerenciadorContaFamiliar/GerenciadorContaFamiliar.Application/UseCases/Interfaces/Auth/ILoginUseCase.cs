using GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Auth;

public interface ILoginUseCase
{
    Task<LoginResponse> ExecutarAsync(LoginRequest request, CancellationToken ct = default);
}
