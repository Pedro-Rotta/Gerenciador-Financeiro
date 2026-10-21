using System.Security.Cryptography;
using GerenciadorContaFamiliar.Application.DTOs.Requests.Auth;
using GerenciadorContaFamiliar.Application.DTOs.Responses.Auth;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Auth;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Auth;

public class EsqueciSenhaUseCase : IEsqueciSenhaUseCase
{
    public const string Mensagem = "Se a conta existir, enviamos um código para o e-mail cadastrado.";

    private readonly IMembroRepository _membroRepository;
    private readonly ICodigoRecuperacaoSenhaRepository _codigoRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEnvioDeEmail _envioDeEmail;
    private readonly IUnitOfWork _unitOfWork;

    public EsqueciSenhaUseCase(
        IMembroRepository membroRepository,
        ICodigoRecuperacaoSenhaRepository codigoRepository,
        IPasswordHasher passwordHasher,
        IEnvioDeEmail envioDeEmail,
        IUnitOfWork unitOfWork)
    {
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _codigoRepository = codigoRepository ?? throw new ArgumentNullException(nameof(codigoRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _envioDeEmail = envioDeEmail ?? throw new ArgumentNullException(nameof(envioDeEmail));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MensagemResponse> ExecutarAsync(EsqueciSenhaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // Conta inexistente ou inativa: a mesma resposta, sem revelar quem tem cadastro
        var membro = await ContaDoIdentificador.AcharAsync(_membroRepository, request.Identificador, ct);
        if (membro is null || !membro.Ativo)
            return new MensagemResponse { Mensagem = Mensagem };

        // Limite por conta (1 por minuto, 5 por hora): acima dele, a mesma resposta e nenhum código
        var agora = DateTime.UtcNow;
        if (await _codigoRepository.ContarCriadosDesdeAsync(membro.Id, agora - CodigoRecuperacaoSenha.IntervaloEntrePedidos, ct) > 0 ||
            await _codigoRepository.ContarCriadosDesdeAsync(membro.Id, agora.AddHours(-1), ct) >= CodigoRecuperacaoSenha.PedidosPorHora)
            return new MensagemResponse { Mensagem = Mensagem };

        // Um pedido novo anula os códigos anteriores; só o hash é guardado
        await _codigoRepository.InvalidarDoMembroAsync(membro.Id, ct);
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        await _codigoRepository.AdicionarAsync(
            new CodigoRecuperacaoSenha(membro.Id, _passwordHasher.Hash(codigo), agora), ct);
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            await _envioDeEmail.EnviarAsync(
                membro.Email,
                "Código para criar uma nova senha",
                $"Seu código para criar uma nova senha no Gerenciador Familiar é {codigo}. " +
                $"Ele vale por {CodigoRecuperacaoSenha.MinutosDeValidade} minutos. " +
                "Se não foi você, ignore este e-mail.",
                ct);
        }
        catch (Exception)
        {
            // Falha no envio já foi registrada no log; a resposta continua a mesma
        }

        return new MensagemResponse { Mensagem = Mensagem };
    }
}
