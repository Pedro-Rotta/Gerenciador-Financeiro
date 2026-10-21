using System.ComponentModel.DataAnnotations;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Relatorios;

/// <summary>
/// Parâmetros do relatório de gastos.
/// Exemplos do domínio:
///  - "Gastos com Combustível"   → informar IdSubCategoria = Combustível (agrupa POR CATEGORIA)
///  - "Gastos com Carro"         → informar IdCategoria = Carro (agrupa POR SUBCATEGORIA)
///  - "Somente Carro sem categoria" → informar IdCategoria = Carro + ApenasSemSubcategoria = true
/// </summary>
public class RelatorioGastosRequest
{
    /// <summary>
    /// Filtro por categoria (opcional). Quando informado, o relatório agrupa por subcategoria.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ID da categoria inválido")]
    public int? IdCategoria { get; set; }

    /// <summary>
    /// Filtro por subcategoria (opcional). Quando informado, o relatório agrupa por categoria.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "ID da subcategoria inválido")]
    public int? IdSubCategoria { get; set; }

    /// <summary>
    /// Se true, considera apenas transações SEM subcategoria ("Carro sem categoria").
    /// </summary>
    public bool ApenasSemSubcategoria { get; set; }

    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
