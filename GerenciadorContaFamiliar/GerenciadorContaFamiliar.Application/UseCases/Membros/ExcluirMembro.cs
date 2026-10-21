using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;
namespace GerenciadorContaFamiliar.Application.UseCases.Membros;

/// <summary>
/// Excluir a própria conta (confirmada com a senha). Os dados ficam no banco: o membro é
/// inativado, sai das contas conjuntas de que participa, as séries das contas dele param de
/// repetir e todas as sessões caem. Quem criou contas conjuntas ativas precisa excluí-las antes.
/// </summary>
public class ExcluirMembroUseCase : IExcluirMembroUseCase
{
    private readonly IMembroRepository _membroRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IRecorrenciaRepository _recorrenciaRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ExcluirMembroUseCase(
        IMembroRepository membroRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        IRecorrenciaRepository recorrenciaRepository,
        INotificacaoRepository notificacaoRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _recorrenciaRepository = recorrenciaRepository ?? throw new ArgumentNullException(nameof(recorrenciaRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MembroResponse> ExecutarAsync(
        ExcluirMembroRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar o membro logado e conferir a senha
        var membro = await _membroRepository.ObterPorIdAsync(request.MembroId, ct)
            ?? throw new KeyNotFoundException("Membro não encontrado.");

        if (!membro.Ativo)
            throw new InvalidOperationException("Membro já está inativo.");

        if (!membro.VerificarSenha(request.Senha, _passwordHasher))
            throw new ArgumentException("Senha incorreta.");

        // 2. O criador não sai da conjunta: ele precisa excluí-la antes
        var conjuntas = await _contaConjuntaRepository.ObterPorMembroAsync(membro.Id, ct);
        var criadas = conjuntas.Where(cc => cc.CriadorId == membro.Id).Select(cc => $"\"{cc.Nome}\"").ToList();
        if (criadas.Count > 0)
            throw new InvalidOperationException(
                $"Você criou contas conjuntas ativas ({string.Join(", ", criadas)}). Exclua-as antes de excluir a sua conta.");

        // 3. Sair das conjuntas de que participa (mesma regra de "sair da conta conjunta")
        foreach (var resumo in conjuntas)
        {
            var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(resumo.Id, ct)
                ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");
            contaConjunta.RemoverMembro(membro);

            foreach (var serie in await _recorrenciaRepository.ObterAtivasPorContaConjuntaAsync(contaConjunta.Id, membro.Id, ct))
                serie.Parar();

            var criador = contaConjunta.Membros.FirstOrDefault(m => m.Id == contaConjunta.CriadorId);
            if (criador is not null)
                await _notificacaoRepository.AdicionarAsync(new Notificacao(
                    $"{membro.Nome} saiu da conta conjunta \"{contaConjunta.Nome}\".",
                    criador,
                    Notificacao.TipoNotificacao.CONTA_CONJUNTA,
                    Notificacao.GravidadeNotificacao.ATENCAO), ct);

            await _contaConjuntaRepository.AtualizarAsync(contaConjunta, ct);
        }

        // 4. As séries das contas dele param de repetir
        foreach (var conta in await _contaRepository.ObterPorMembroAsync(membro.Id, ct))
            foreach (var serie in await _recorrenciaRepository.ObterAtivasPorContaAsync(conta.Id, ct))
                serie.Parar();

        // 5. Inativar e derrubar todas as sessões
        membro.Inativar();
        membro.EncerrarSessoes();

        // 6. Persistir tudo junto
        await _membroRepository.AtualizarAsync(membro, ct);
        await _unitOfWork.SaveChangesAsync(ct);

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
