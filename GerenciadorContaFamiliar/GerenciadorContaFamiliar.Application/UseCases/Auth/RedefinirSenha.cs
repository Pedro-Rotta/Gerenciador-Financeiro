using GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Auth;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Auth;

public class RedefinirSenhaUseCase : IRedefinirSenhaUseCase
{
    public const string CodigoInvalido = "Código inválido ou expirado.";

    private readonly IMembroRepository _membroRepository;
    private readonly ICodigoRecuperacaoSenhaRepository _codigoRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RedefinirSenhaUseCase(
        IMembroRepository membroRepository,
        ICodigoRecuperacaoSenhaRepository codigoRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _codigoRepository = codigoRepository ?? throw new ArgumentNullException(nameof(codigoRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MensagemResponse> ExecutarAsync(RedefinirSenhaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (request.NovaSenha != request.ConfirmacaoSenha)
            throw new ArgumentException("As senhas não coincidem");

        var membro = await ContaDoIdentificador.AcharAsync(_membroRepository, request.Identificador, ct);
        var codigo = membro is null ? null : await _codigoRepository.ObterUltimoDoMembroAsync(membro.Id, ct);

        // Conta inexistente, sem código, expirado, usado ou com 5 tentativas: a mesma mensagem.
        // A tentativa é gasta no banco antes de conferir: palpites em paralelo não passam de 5
        var agora = DateTime.UtcNow;
        if (membro is null || !membro.Ativo || codigo is null || !codigo.EstaValido(agora) ||
            !await _codigoRepository.ReservarTentativaAsync(codigo.Id, agora, ct))
            throw new ArgumentException(CodigoInvalido);

        if (!_passwordHasher.Verify(request.Codigo, codigo.CodigoHash) ||
            !await _codigoRepository.MarcarUsadoSeAbertoAsync(codigo.Id, ct))
            throw new ArgumentException(CodigoInvalido);

        // Membro rastreado (a autenticação pode já ter carregado esta conta, se houver sessão aberta);
        // senha nova e versão da sessão nova: as sessões abertas caem
        var rastreado = await _membroRepository.ObterPorIdAsync(membro.Id, ct)
            ?? throw new ArgumentException(CodigoInvalido);
        rastreado.TrocarSenha(request.NovaSenha, _passwordHasher);
        await _membroRepository.AtualizarAsync(rastreado, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new MensagemResponse { Mensagem = "Senha alterada. Entre com a nova senha." };
    }
}
