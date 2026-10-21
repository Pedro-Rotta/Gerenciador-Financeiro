using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Membros;

/// <summary>
/// Alterar senha (logado): confere a senha atual, troca e devolve um token novo para o navegador
/// atual continuar logado; as outras sessões caem (versão da sessão sobe).
/// </summary>
public class AlterarSenhaUseCase : IAlterarSenhaUseCase
{
    private readonly IMembroRepository _membroRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public AlterarSenhaUseCase(
        IMembroRepository membroRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<LoginResponse> ExecutarAsync(AlterarSenhaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (request.NovaSenha != request.ConfirmacaoSenha)
            throw new ArgumentException("As senhas não coincidem");

        var membro = await _membroRepository.ObterPorIdAsync(request.MembroId, ct)
            ?? throw new KeyNotFoundException("Membro não encontrado.");

        if (!membro.VerificarSenha(request.SenhaAtual, _passwordHasher))
            throw new ArgumentException("Senha atual incorreta.");

        membro.TrocarSenha(request.NovaSenha, _passwordHasher);
        await _membroRepository.AtualizarAsync(membro, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var (token, expiraEm) = _tokenService.GerarToken(membro.Id, membro.Email, membro.VersaoSessao);
        return new LoginResponse
        {
            MembroId = membro.Id,
            Nome = membro.Nome,
            NomeDeUsuario = membro.NomeDeUsuario,
            Email = membro.Email,
            Token = token,
            ExpiraEm = expiraEm
        };
    }
}
