using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Recorrencias;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Verificacao;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Recorrencias;

/// <summary>
/// Passo da verificação: gera as ocorrências das séries ativas das contas que o membro acessa,
/// até <see cref="GeradorDeOcorrencias.DiasDeAntecedencia"/> dias à frente.
/// </summary>
public class GerarOcorrenciasRecorrentes : IPassoVerificacao
{
    private readonly IRecorrenciaRepository _recorrenciaRepository;
    private readonly IGeradorDeOcorrencias _geradorDeOcorrencias;
    private readonly IUnitOfWork _unitOfWork;

    public GerarOcorrenciasRecorrentes(
        IRecorrenciaRepository recorrenciaRepository,
        IGeradorDeOcorrencias geradorDeOcorrencias,
        IUnitOfWork unitOfWork)
    {
        _recorrenciaRepository = recorrenciaRepository ?? throw new ArgumentNullException(nameof(recorrenciaRepository));
        _geradorDeOcorrencias = geradorDeOcorrencias ?? throw new ArgumentNullException(nameof(geradorDeOcorrencias));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public string Nome => "Gerar ocorrências recorrentes";

    public async Task ExecutarAsync(int membroId, CancellationToken ct = default)
    {
        var series = await _recorrenciaRepository.ObterAtivasPorMembroAsync(membroId, ct);

        foreach (var serie in series)
        {
            // A série para sozinha se a conta foi excluída ou se o autor saiu da conta conjunta
            if ((serie.Conta is null && serie.ContaConjunta is null) ||
                (serie.ContaConjunta is not null && !serie.ContaConjunta.Membros.Any(m => m.Id == serie.Membro.Id)))
            {
                serie.Parar();
                continue;
            }

            // Conta individual ainda sem saldo inicial não aceita transações: a série espera
            // (sem travar as outras) e gera o que faltou quando o saldo inicial for informado
            if (serie.Conta is not null && !serie.Conta.SaldoInicialDefinido)
                continue;

            await _geradorDeOcorrencias.GerarAsync(serie, ct);
        }

        // Uma gravação para tudo; o índice único (série + data) barra duplicação entre
        // verificações simultâneas de participantes da mesma conta conjunta
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
