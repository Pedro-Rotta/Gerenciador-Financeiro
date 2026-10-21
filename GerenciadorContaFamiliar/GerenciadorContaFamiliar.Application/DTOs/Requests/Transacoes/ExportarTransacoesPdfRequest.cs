using System.ComponentModel.DataAnnotations;
using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.DTOs.Requests.Transacoes;

/// <summary>
/// Filtros do PDF de transações — os mesmos da tabela "Transações recentes" do Dashboard.
/// Todos opcionais: sem filtro, entram todas as transações das contas do membro.
/// </summary>
public class ExportarTransacoesPdfRequest
{
    /// <summary>Só entradas ou só saídas; vazio = todas.</summary>
    public Transacao.TipoTransacao? Tipo { get; set; }

    /// <summary>Intervalo de datas (inclusivo); vazio = sem limite.</summary>
    public DateOnly? DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }

    /// <summary>Uma conta individual OU uma conta conjunta; vazio = todas as contas.</summary>
    public int? ContaId { get; set; }
    public int? ContaConjuntaId { get; set; }

    /// <summary>Nome da categoria (a mesma categoria em contas diferentes conta junto).</summary>
    [StringLength(100, ErrorMessage = "A categoria deve ter no máximo 100 caracteres")]
    public string? Categoria { get; set; }

    /// <summary>Rótulo do período escolhido na tela (ex.: "Esta semana"), só para exibição.</summary>
    [StringLength(50, ErrorMessage = "O período deve ter no máximo 50 caracteres")]
    public string? Periodo { get; set; }

    /// <summary>Mesma ordem da tabela: "conta,valor:inv" (colunas na ordem em que foram marcadas).</summary>
    [StringLength(100, ErrorMessage = "A ordenação deve ter no máximo 100 caracteres")]
    public string? Ordenacao { get; set; }

    /// <summary>Injetado automaticamente pelo controller a partir do JWT.</summary>
    public int MembroId { get; set; }
}
