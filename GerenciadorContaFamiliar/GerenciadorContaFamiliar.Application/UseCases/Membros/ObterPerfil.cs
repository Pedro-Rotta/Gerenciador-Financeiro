using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Membros;

public class ObterPerfilUseCase : IObterPerfilUseCase
{
    private readonly IMembroRepository _membroRepository;

    public ObterPerfilUseCase(IMembroRepository membroRepository)
    {
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
    }

    public async Task<MembroResponse> ExecutarAsync(int membroId, CancellationToken ct = default)
    {
        var membro = await _membroRepository.ObterPorIdAsync(membroId, ct)
            ?? throw new KeyNotFoundException("Membro não encontrado.");

        return new MembroResponse
        {
            Id = membro.Id,
            Nome = membro.Nome,
            NomeDeUsuario = membro.NomeDeUsuario,
            Email = membro.Email,
            Telefone = membro.Telefone,
            DataNascimento = membro.DataNascimento,
            Ativo = membro.Ativo,
            CriadoEm = membro.CriadoEm,
            ContaPrincipalId = membro.ContaPrincipalId
        };
    }
}
