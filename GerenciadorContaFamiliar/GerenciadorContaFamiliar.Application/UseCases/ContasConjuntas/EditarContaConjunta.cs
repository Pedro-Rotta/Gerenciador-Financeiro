using GerenciadorContaFamiliar.Application.DTOs.Requests.ContasConjuntas;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.ContasConjuntas;
using GerenciadorContaFamiliar.Application.UseCases.Notificacoes;
using GerenciadorContaFamiliar.Application.Exceptions;

namespace GerenciadorContaFamiliar.Application.UseCases.ContasConjuntas;


public class EditarContaConjuntaUseCase : IEditarContaConjuntaUseCase
{
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EditarContaConjuntaUseCase(
        IContaConjuntaRepository contaConjuntaRepository,
        INotificacaoRepository notificacaoRepository,
        IUnitOfWork unitOfWork)
    {
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ContaConjuntaResponse> ExecutarAsync(
        EditarContaConjuntaRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Buscar a entidade real pelo Id
        var contaConjunta = await _contaConjuntaRepository.ObterPorIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException("ContaConjunta não encontrada.");

        // 2. Verificar propriedade: só o criador pode editar
        if (contaConjunta.CriadorId != request.MembroId)
            throw new AcessoNegadoException("Você não tem permissão para editar esta conta conjunta.");

        // 3. Atualizar somente o que foi enviado
        var nomeAnterior = contaConjunta.Nome;
        if (request.Nome is not null)
            contaConjunta.AtualizarNome(request.Nome);
        // Descrição vazia ("") limpa o campo
        if(request.Descricao is not null)
            contaConjunta.AtualizarDescricao(
                string.IsNullOrWhiteSpace(request.Descricao) ? null : request.Descricao.Trim());

        // Avisar os participantes quando a conta é renomeada
        if (contaConjunta.Nome != nomeAnterior)
        {
            var criador = contaConjunta.Membros.First(m => m.Id == contaConjunta.CriadorId);
            await AvisosNotificacao.AvisarOutrosParticipantesAsync(
                _notificacaoRepository, contaConjunta, criador.Id,
                $"{criador.Nome} renomeou a conta conjunta \"{nomeAnterior}\" para \"{contaConjunta.Nome}\".",
                Notificacao.GravidadeNotificacao.INFORMATIVA, ct);
        }

        // 3. Persistir
        await _contaConjuntaRepository.AtualizarAsync(contaConjunta, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 4. Mapear resposta
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
