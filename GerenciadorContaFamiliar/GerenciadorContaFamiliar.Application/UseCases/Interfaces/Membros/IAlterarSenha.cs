using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;

public interface IAlterarSenhaUseCase
{
    /// <summary>Troca a senha do membro logado e devolve a sessão nova (as outras caem).</summary>
    Task<LoginResponse> ExecutarAsync(AlterarSenhaRequest request, CancellationToken ct = default);
}
