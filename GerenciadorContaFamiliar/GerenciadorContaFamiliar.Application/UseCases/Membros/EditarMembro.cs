using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;
using GerenciadorContaFamiliar.Application.Exceptions;
namespace GerenciadorContaFamiliar.Application.UseCases.Membros;

public class EditarMembroUseCase : IEditarMembroUseCase
{
    private readonly IMembroRepository _membroRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EditarMembroUseCase(IMembroRepository membroRepository, IUnitOfWork unitOfWork)
    {
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MembroResponse> ExecutarAsync(
        EditarMembroRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var membro = await _membroRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Membro não encontrado.");

        // 0. Verificar propriedade: só pode editar a si mesmo
        if (membro.Id != request.Id)
            throw new AcessoNegadoException("Você não tem permissão para editar outro membro.");

        // 2. Validar NomeDeUsuario se foi alterado
        if (!string.IsNullOrWhiteSpace(request.NomeDeUsuario) && request.NomeDeUsuario != membro.NomeDeUsuario)
        {
            var existente = await _membroRepository.ObterPorNomeDeUsuarioAsync(request.NomeDeUsuario, ct);
            if (existente is not null)
                throw new ArgumentException("Já existe um membro com este nome de usuário.");
            membro.AtualizarNomeDeUsuario(request.NomeDeUsuario);
        }

        // 3. Validar Email se foi alterado
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != membro.Email)
        {
            var existente = await _membroRepository.ObterPorEmailAsync(request.Email, ct);
            // A busca ignora maiúsculas: o próprio membro pode só mudar a caixa do e-mail
            if (existente is not null && existente.Id != membro.Id)
                throw new ArgumentException("Já existe um membro com este email.");
            membro.AtualizarEmail(request.Email);
        }

        // 4. Atualizar somente o que foi enviado
        if (request.Nome is not null)
            membro.AtualizarNome(request.Nome);
        if (request.Telefone is not null)
            membro.AtualizarTelefone(request.Telefone);
        if (request.DataNascimento.HasValue)
            membro.AtualizarDataNascimento(request.DataNascimento.Value);

        // 5. Persistir
        await _membroRepository.AtualizarAsync(membro, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 6. Mapear resposta
        return new MembroResponse
        {
            Id = membro.Id, 
            Nome = membro.Nome, 
            NomeDeUsuario = membro.NomeDeUsuario,
            Email = membro.Email,
            Telefone = membro.Telefone, 
            DataNascimento = membro.DataNascimento,
            Ativo = membro.Ativo, 
            CriadoEm = membro.CriadoEm
        };
    }
}
