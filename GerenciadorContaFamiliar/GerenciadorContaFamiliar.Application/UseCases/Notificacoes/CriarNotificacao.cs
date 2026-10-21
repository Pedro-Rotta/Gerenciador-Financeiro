using GerenciadorContaFamiliar.Application.DTOs.Requests.Notificacoes;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Notificacoes;

namespace GerenciadorContaFamiliar.Application.UseCases.Notificacoes;

public class CriarNotificacaoUseCase : ICriarNotificacaoUseCase
{
    private readonly INotificacaoRepository _notificacaoRepository;
    private readonly IMembroRepository _membroRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CriarNotificacaoUseCase(
        INotificacaoRepository notificacaoRepository,
        IMembroRepository membroRepository,
        IUnitOfWork unitOfWork)
    {
        _notificacaoRepository = notificacaoRepository ?? throw new ArgumentNullException(nameof(notificacaoRepository));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<NotificacaoResponse> ExecutarAsync(
        CriarNotificacaoRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1️⃣ Validação de entrada: carregar dependências
        var membro = await _membroRepository.ObterPorIdAsync(request.MembroId, ct)
            ?? throw new ArgumentException("Membro não encontrado.");

        // 2️⃣ Criar entidade de domínio
        var notificacao = new Notificacao(request.Mensagem, membro);

        // 3️⃣ Persistir via repositório
        await _notificacaoRepository.AdicionarAsync(notificacao, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // 4️⃣ Mapear para DTO de saída
        return new NotificacaoResponse
        {
            Id = notificacao.Id,
            Mensagem = notificacao.Mensagem,
            Lida = notificacao.Lida,
            Tipo = notificacao.Tipo,
            Gravidade = notificacao.Gravidade,
            MembroId = notificacao.Membro.Id,
            CriadoEm = notificacao.CriadoEm
        };
    }
}
