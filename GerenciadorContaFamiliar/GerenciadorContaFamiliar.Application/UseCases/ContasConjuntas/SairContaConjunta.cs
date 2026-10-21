using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.ContasConjuntas;

/// <summary>
/// Um participante (que não seja o criador) sai da conta conjunta; o criador recebe uma notificação.
/// As transações que ele lançou continuam no histórico da conta.
/// </summary>
public class SairContaConjuntaUseCase : ISairContaConjuntaUseCase
{
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IRecorrenciaRepository _recorrenciaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SairContaConjuntaUseCase(
        IContaConjuntaRepository contaConjuntaRepository,
        INotificacaoRepository notificacaoRepository,
        IRecorrenciaRepository recorrenciaRepository,
        IUnitOfWork unitOfWork)
    {
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _recorrenciaRepository = recorrenciaRepository ?? throw new ArgumentNullException(nameof(recorrenciaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ContaConjuntaResponse> ExecutarAsync(
        SairContaConjuntaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a conta conjunta (com os participantes)
        var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("Conta conjunta não encontrada.");

        // 2. Verificar participação: só quem participa pode sair
        var membro = contaConjunta.Membros.FirstOrDefault(m => m.Id == request.MembroId)
            ?? throw new AcessoNegadoException("Você não participa desta conta conjunta.");

        // 3. Sair (o domínio impede o criador de sair; ele deve excluir a conta)
        contaConjunta.RemoverMembro(membro);

        // As séries recorrentes que ele criou nesta conta param de repetir
        foreach (var serie in await _recorrenciaRepository.ObterAtivasPorContaConjuntaAsync(contaConjunta.Id, membro.Id, ct))
            serie.Parar();

        // 4. Avisar o criador
        var criador = contaConjunta.Membros.FirstOrDefault(m => m.Id == contaConjunta.CriadorId);
        if (criador is not null)
        {
            var notificacao = new Notificacao(
                $"{membro.Nome} saiu da conta conjunta \"{contaConjunta.Nome}\".",
                criador,
                Notificacao.TipoNotificacao.CONTA_CONJUNTA,
                Notificacao.GravidadeNotificacao.ATENCAO);
            await _notificacaoRepository.AdicionarAsync(notificacao, ct);
        }

        // 5. Persistir tudo junto
        await _contaConjuntaRepository.AtualizarAsync(contaConjunta, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 6. Mapear resposta
        return new ContaConjuntaResponse
        {
            Id = contaConjunta.Id,
            Nome = contaConjunta.Nome,
            Saldo = contaConjunta.Saldo,
            Descricao = contaConjunta.Descricao,
            CriadorId = contaConjunta.CriadorId,
            Ativo = contaConjunta.Ativo,
            Membros = contaConjunta.Membros
                .Select(m => new MembroResumoResponse { Id = m.Id, Nome = m.Nome, NomeDeUsuario = m.NomeDeUsuario })
                .ToList(),
        };
    }
}
