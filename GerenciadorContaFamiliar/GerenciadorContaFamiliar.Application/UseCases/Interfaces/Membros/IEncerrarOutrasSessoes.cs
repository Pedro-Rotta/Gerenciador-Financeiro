using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;

public interface IEncerrarOutrasSessoesUseCase
{
    /// <summary>Derruba todas as sessões e devolve uma nova para o navegador atual.</summary>
    Task<LoginResponse> ExecutarAsync(int membroId, CancellationToken ct = default);
}
