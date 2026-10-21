using GerenciadorContaFamiliar.Application.DTOs.Requests.Membros;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Membros;
using GerenciadorContaFamiliar.Domain;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;

namespace GerenciadorContaFamiliar.Application.UseCases.Membros;

public class SalvarPreferenciasUsoUseCase : ISalvarPreferenciasUsoUseCase
{
    private readonly IPreferenciaUsoRepository _preferenciaRepository;
    private readonly IContaRepository _contaRepository;
    private readonly IContaConjuntaRepository _contaConjuntaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SalvarPreferenciasUsoUseCase(
        IPreferenciaUsoRepository preferenciaRepository,
        IContaRepository contaRepository,
        IContaConjuntaRepository contaConjuntaRepository,
        IUnitOfWork unitOfWork)
    {
        _contaRepository = contaRepository ?? throw new ArgumentNullException(nameof(contaRepository));
        _contaConjuntaRepository = contaConjuntaRepository ?? throw new ArgumentNullException(nameof(contaConjuntaRepository));
        _preferenciaRepository = preferenciaRepository ?? throw new ArgumentNullException(nameof(preferenciaRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PreferenciasUsoResponse> ExecutarAsync(SalvarPreferenciasUsoRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (!string.IsNullOrWhiteSpace(request.ContaPadrao) && !await ContaDoMembroAsync(request.ContaPadrao, request.MembroId, ct))
            throw new ArgumentException("Conta padrão inválida.");

        var preferencia = await _preferenciaRepository.ObterPorMembroAsync(request.MembroId, ct);
        if (preferencia is null)
        {
            preferencia = new PreferenciaUso(request.MembroId);
            await _preferenciaRepository.AdicionarAsync(preferencia, ct);
        }

        preferencia.Atualizar(request.SemanaComecaNoDomingo, request.ContaPadrao, request.PeriodoRelatorios, request.OcultarValores);
        await _unitOfWork.SaveChangesAsync(ct);

        return ObterPreferenciasUsoUseCase.Mapear(preferencia);
    }

    // A conta padrão precisa ser uma conta ativa do membro ("c:ID") ou uma conjunta ativa de que ele participa ("cc:ID")
    private async Task<bool> ContaDoMembroAsync(string chave, int membroId, CancellationToken ct)
    {
        var partes = chave.Split(':');
        if (partes.Length != 2 || !int.TryParse(partes[1], out var id))
            return false;

        if (partes[0] == "c")
        {
            var conta = await _contaRepository.ObterPorIdAsync(id, ct);
            return conta is not null && conta.Ativo && conta.MembroId == membroId;
        }
        if (partes[0] == "cc")
        {
            var conjunta = await _contaConjuntaRepository.ObterPorIdAsync(id, ct);
            return conjunta is not null && conjunta.Ativo && conjunta.Membros.Any(m => m.Id == membroId);
        }
        return false;
    }
}
