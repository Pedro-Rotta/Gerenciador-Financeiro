using GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Auth;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Auth;

public class LoginUseCase : ILoginUseCase
{
    private readonly IMembroRepository _membroRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginUseCase(
        IMembroRepository membroRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    public async Task<LoginResponse> ExecutarAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.NomeDeUsuario))
            throw new ArgumentException("O nome de usuário é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Senha))
            throw new ArgumentException("A senha é obrigatória.");

        // Busca membro pelo nome de usuário
        var membro = await _membroRepository.ObterPorNomeDeUsuarioAsync(request.NomeDeUsuario, ct)
            ?? throw new ArgumentException("Nome de usuário ou senha inválidos.");

        // Verifica se a senha confere
        if (!membro.VerificarSenha(request.Senha, _passwordHasher))
            throw new ArgumentException("Nome de usuário ou senha inválidos.");

        // Verifica se o membro está ativo
        if (!membro.Ativo)
            throw new ArgumentException("Conta desativada. Entre em contato com o suporte.");

        // Gera o token JWT
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
