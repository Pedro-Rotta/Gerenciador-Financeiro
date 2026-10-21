using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Relatorios;

/// <summary>
/// Parâmetros do resumo financeiro (página Relatórios).
/// Sem Ano/Mes, usa o mês atual. Meses define o tamanho do período que termina
/// nesse mês: 1 (mês), 3 (trimestre), 6 (semestre) ou 12 (ano). Padrão: 1.
/// </summary>
public class RelatorioResumoRequest
{
    [Range(2000, 2100, ErrorMessage = "Ano inválido")]
    public int? Ano { get; set; }

    [Range(1, 12, ErrorMessage = "Mês inválido")]
    public int? Mes { get; set; }

    /// <summary>Quantidade de meses do período (1, 3, 6 ou 12), terminando em Ano/Mes.</summary>
    public int? Meses { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
