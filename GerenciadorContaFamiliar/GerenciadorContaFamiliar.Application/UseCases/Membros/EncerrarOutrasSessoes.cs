using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Membros;

/// <summary>
/// "Encerrar outras sessões" (aba Segurança): sobe a versão da sessão, então todos os tokens
/// atuais caem, e devolve um token novo para o navegador que pediu continuar logado.
/// </summary>
public class EncerrarOutrasSessoesUseCase : IEncerrarOutrasSessoesUseCase
{
    private readonly IMembroRepository _membroRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public EncerrarOutrasSessoesUseCase(
        IMembroRepository membroRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<LoginResponse> ExecutarAsync(int membroId, CancellationToken ct = default)
    {
        var membro = await _membroRepository.ObterPorIdAsync(membroId, ct)
            ?? throw new KeyNotFoundException("Membro não encontrado.");

        membro.EncerrarSessoes();
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
