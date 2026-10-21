using GerenciadorContaFamiliar.Application.DTOs.Responses;

namespace GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;

public interface IObterPerfilUseCase
{
    /// <summary>Perfil do membro logado (aba Conta das Configurações).</summary>
    Task<MembroResponse> ExecutarAsync(int membroId, CancellationToken ct = default);
}
