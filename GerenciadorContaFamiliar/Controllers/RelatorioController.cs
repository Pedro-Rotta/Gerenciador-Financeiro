using GerenciadorContaFamiliar.Application.DTOs.Requests.Relatorios;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GerenciadorContaFamiliar.Controllers;

[Authorize]
[Route("api/[controller]")]
public class RelatorioController : BaseApiController
{
    private readonly IRelatorioGastosUseCase _relatorioGastosUseCase;
    private readonly IRelatorioResumoUseCase _relatorioResumoUseCase;
    private readonly IExportarRelatorioPdfUseCase _exportarRelatorioPdfUseCase;

    public RelatorioController(
        IRelatorioGastosUseCase relatorioGastosUseCase,
        IRelatorioResumoUseCase relatorioResumoUseCase,
        IExportarRelatorioPdfUseCase exportarRelatorioPdfUseCase)
    {
        _relatorioGastosUseCase = relatorioGastosUseCase ?? throw new ArgumentNullException(nameof(relatorioGastosUseCase));
        _relatorioResumoUseCase = relatorioResumoUseCase ?? throw new ArgumentNullException(nameof(relatorioResumoUseCase));
        _exportarRelatorioPdfUseCase = exportarRelatorioPdfUseCase ?? throw new ArgumentNullException(nameof(exportarRelatorioPdfUseCase));
    }

    /// <summary>
    /// Relatório de gastos. Exemplos:
    ///  - Gastos com Combustível:        ?IdSubCategoria=3
    ///  - Gastos com Carro:              ?IdCategoria=1
    ///  - Carro sem categoria:           ?IdCategoria=1&amp;ApenasSemSubcategoria=true
    ///  - Período:                       ?IdCategoria=1&amp;DataInicio=2026-01-01&amp;DataFim=2026-12-31
    /// </summary>
    [HttpGet("gastos")]
    public async Task<ActionResult<RelatorioGastosResponse>> Gastos(
        [FromQuery] RelatorioGastosRequest request,
        CancellationToken ct = default)
    {
        // O relatório considera apenas as contas do membro logado (nunca confia no cliente).
        request.MembroId = ObterMembroIdAtual();
        var response = await _relatorioGastosUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// Resumo financeiro do período (página Relatórios): cards do resumo executivo,
    /// despesas do período e comparativo mensal. Meses = 1, 3, 6 ou 12 (padrão 1),
    /// terminando em Ano/Mes. Exemplos:
    ///  - Mês atual:                 /api/Relatorio/resumo
    ///  - Outubro de 2026:           /api/Relatorio/resumo?Ano=2026&amp;Mes=10
    ///  - Trimestre até out/2026:    /api/Relatorio/resumo?Ano=2026&amp;Mes=10&amp;Meses=3
    /// </summary>
    [HttpGet("resumo")]
    public async Task<ActionResult<RelatorioResumoResponse>> Resumo(
        [FromQuery] RelatorioResumoRequest request,
        CancellationToken ct = default)
    {
        // O resumo considera apenas as contas do membro logado (nunca confia no cliente).
        request.MembroId = ObterMembroIdAtual();
        var response = await _relatorioResumoUseCase.ExecutarAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// PDF do resumo do período (mesmos parâmetros de /resumo), para download.
    /// Ex.: /api/Relatorio/resumo/pdf?Ano=2026&amp;Mes=10&amp;Meses=3
    /// </summary>
    [HttpGet("resumo/pdf")]
    public async Task<IActionResult> ResumoPdf(
        [FromQuery] RelatorioResumoRequest request,
        CancellationToken ct = default)
    {
        request.MembroId = ObterMembroIdAtual();
        var pdf = await _exportarRelatorioPdfUseCase.ExecutarAsync(request, ct);
        return File(pdf.Conteudo, "application/pdf", pdf.NomeArquivo);
    }
}
