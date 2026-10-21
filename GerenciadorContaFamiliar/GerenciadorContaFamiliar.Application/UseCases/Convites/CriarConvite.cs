using GerenciadorContaFamiliar.Application.DTOs.Requests.Convites;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Convites;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.Convites;

/// <summary>
/// Convida um membro (por nome de usuário ou e-mail) para participar de uma conta conjunta.
/// Só o criador da conta pode convidar.
/// </summary>
public class CriarConviteUseCase : ICriarConviteUseCase
{
    private readonly IConviteContaConjuntaRepository _conviteRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarConviteUseCase(
        IConviteContaConjuntaRepository conviteRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        IMembroRepository membroRepository,
        IUnitOfWork unitOfWork)
    {
        _conviteRepository = conviteRepository ?? throw new ArgumentNullException(nameof(conviteRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ConviteResponse> ExecutarAsync(
        CriarConviteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var usuarioOuEmail = request.UsuarioOuEmail?.Trim();
        if (string.IsNullOrWhiteSpace(usuarioOuEmail))
            throw new ArgumentException("Informe o nome de usuário ou o e-mail do convidado.");

        // 1️⃣ Buscar a conta conjunta e verificar propriedade: só o criador convida
        var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.ContaConjuntaId, cancellationToken)
            ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");

        if (contaConjunta.CriadorId != request.MembroId)
            throw new AcessoNegadoException("Só o criador da conta conjunta pode convidar participantes.");

        // 2️⃣ Buscar o convidado: por e-mail (se tiver "@") ou por nome de usuário
        var convidado = usuarioOuEmail.Contains('@')
            ? await _membroRepository.ObterPorEmailAsync(usuarioOuEmail, cancellationToken)
            : await _membroRepository.ObterPorNomeDeUsuarioAsync(usuarioOuEmail, cancellationToken);

        if (convidado is null)
            throw new ArgumentException("Nenhum membro encontrado com este nome de usuário ou e-mail.");

        // 3️⃣ Regras: não convidar a si mesmo, quem já participa, nem repetir convite pendente
        if (convidado.Id == request.MembroId)
            throw new ArgumentException("Você não pode convidar a si mesmo.");

        if (contaConjunta.Membros.Any(m => m.Id == convidado.Id))
            throw new InvalidOperationException("Este membro já participa da conta conjunta.");

        if (await _conviteRepository.ExistePendenteAsync(contaConjunta.Id, convidado.Id, cancellationToken))
            throw new InvalidOperationException("Já existe um convite pendente para este membro nesta conta.");

        // 4️⃣ Criar entidade de domínio e persistir
        var convite = new ConviteContaConjunta(contaConjunta.Id, convidado.Id, request.MembroId);

        await _conviteRepository.AdicionarAsync(convite, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5️⃣ Mapear para DTO de saída
        var criador = contaConjunta.Membros.FirstOrDefault(m => m.Id == request.MembroId);
        return new ConviteResponse
        {
            Id = convite.Id,
            ContaConjuntaId = contaConjunta.Id,
            ContaConjuntaNome = contaConjunta.Nome,
            ConvidadoId = convidado.Id,
            ConvidadoNome = convidado.Nome,
            ConvidadoNomeDeUsuario = convidado.NomeDeUsuario,
            ConvidadoPorId = request.MembroId,
            ConvidadoPorNome = criador?.Nome ?? string.Empty,
            Status = convite.Status,
            CriadoEm = convite.CriadoEm,
            RespondidoEm = convite.RespondidoEm,
        };
    }
}
