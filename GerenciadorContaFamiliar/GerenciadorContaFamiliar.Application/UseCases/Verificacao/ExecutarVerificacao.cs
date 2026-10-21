using GerenciadorContaFamiliar.Application.DTOs.Requests.Verificacao;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Verificacao;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Verificacao;

/// <summary>
/// Verificação ao abrir o app: roda os passos (gerar ocorrências das séries etc.) no máximo
/// uma vez por hora para cada membro. A falha de um passo não bloqueia os seguintes.
/// </summary>
public class ExecutarVerificacaoUseCase : IExecutarVerificacaoUseCase
{
    private readonly IEnumerable<IPassoVerificacao> _passos;
    private readonly ILimiteVerificacao _limiteVerificacao;
    private readonly IUnitOfWork _unitOfWork;

    public ExecutarVerificacaoUseCase(
        IEnumerable<IPassoVerificacao> passos,
        ILimiteVerificacao limiteVerificacao,
        IUnitOfWork unitOfWork)
    {
        _passos = passos ?? throw new ArgumentNullException(nameof(passos));
        _limiteVerificacao = limiteVerificacao ?? throw new ArgumentNullException(nameof(limiteVerificacao));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<VerificacaoResponse> ExecutarAsync(
        ExecutarVerificacaoRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var resposta = new VerificacaoResponse();
        if (!_limiteVerificacao.TentarReservar(request.MembroId))
            return resposta;

        resposta.Executada = true;
        foreach (var passo in _passos)
        {
            try
            {
                await passo.ExecutarAsync(request.MembroId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Descarta o que o passo deixou pela metade, para o próximo não gravar junto
                _unitOfWork.DescartarAlteracoes();
                resposta.Falhas.Add($"{passo.Nome}: {ex.GetBaseException().Message}");
            }
        }
        return resposta;
    }
}
