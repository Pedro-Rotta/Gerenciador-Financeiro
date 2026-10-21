using GerenciadorContaFamiliar.Application.DTOs.Requests.Relatorios;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;

namespace GerenciadorContaFamiliar.Application.UseCases.Relatorios;

/// <summary>
/// PDF do relatório financeiro do período (mesmos dados da página Relatórios):
/// reaproveita o resumo e entrega o arquivo pronto para download.
/// </summary>
public class ExportarRelatorioPdfUseCase : IExportarRelatorioPdfUseCase
{
    private readonly IRelatorioResumoUseCase _relatorioResumoUseCase;
    private readonly IMembroRepository _membroRepository;
    private readonly IGeradorPdfRelatorio _geradorPdf;

    public ExportarRelatorioPdfUseCase(
        IRelatorioResumoUseCase relatorioResumoUseCase,
        IMembroRepository membroRepository,
        IGeradorPdfRelatorio geradorPdf)
    {
        _relatorioResumoUseCase = relatorioResumoUseCase ?? throw new ArgumentNullException(nameof(relatorioResumoUseCase));
        _membroRepository = membroRepository ?? throw new ArgumentNullException(nameof(membroRepository));
        _geradorPdf = geradorPdf ?? throw new ArgumentNullException(nameof(geradorPdf));
    }

    public async Task<RelatorioPdfResponse> ExecutarAsync(
        RelatorioResumoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // 1. Mesmo resumo da tela (valida período, mês futuro etc.)
        var resumo = await _relatorioResumoUseCase.ExecutarAsync(request, cancellationToken);

        // 2. Nome do membro para o cabeçalho do PDF
        var membro = await _membroRepository.ObterPorIdAsync(request.MembroId, cancellationToken)
            ?? throw new KeyNotFoundException("Membro não encontrado.");

        // 3. Gerar o arquivo
        var fim = $"{resumo.DataFim:yyyy-MM}";
        var nomeArquivo = resumo.Meses == 1
            ? $"relatorio-{fim}.pdf"
            : $"relatorio-{resumo.DataInicio:yyyy-MM}-a-{fim}.pdf";

        return new RelatorioPdfResponse
        {
            Conteudo = _geradorPdf.Gerar(resumo, membro.Nome),
            NomeArquivo = nomeArquivo,
        };
    }
}
