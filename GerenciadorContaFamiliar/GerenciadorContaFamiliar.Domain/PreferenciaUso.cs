using System.Text.RegularExpressions;
using GerenciadorContaFamiliar.Domain.Shared;

namespace GerenciadorContaFamiliar.Domain;

/// <summary>
/// Preferências de uso do app por membro (Configurações → Preferências): primeiro dia da semana,
/// conta padrão para novas transações, período padrão dos Relatórios e ocultar valores ao abrir.
/// </summary>
public class PreferenciaUso : BaseEntity
{
    public static readonly int[] PeriodosPermitidos = { 1, 3, 6, 12 };

    public int MembroId { get; private set; }

    /// <summary>Falso = a semana começa na segunda (padrão); verdadeiro = no domingo.</summary>
    public bool SemanaComecaNoDomingo { get; private set; }

    /// <summary>Conta que já vem escolhida ao lançar: "c:ID" (individual) ou "cc:ID" (conjunta); null = a principal.</summary>
    public string? ContaPadrao { get; private set; }

    /// <summary>Período inicial dos Relatórios, em meses (1, 3, 6 ou 12).</summary>
    public int PeriodoRelatorios { get; private set; } = 1;

    /// <summary>Mostra os valores do painel como •••• até a pessoa tocar para ver.</summary>
    public bool OcultarValores { get; private set; }

    // Construtor privado usado pelo EF Core ao materializar entidades do banco.
    private PreferenciaUso() { }

    public PreferenciaUso(int membroId)
    {
        if (membroId <= 0)
            throw new ArgumentException("O membro é obrigatório.");
        MembroId = membroId;
    }

    public void Atualizar(bool semanaComecaNoDomingo, string? contaPadrao, int periodoRelatorios, bool ocultarValores)
    {
        if (!PeriodosPermitidos.Contains(periodoRelatorios))
            throw new ArgumentException("O período dos Relatórios deve ser 1, 3, 6 ou 12 meses.");
        var conta = string.IsNullOrWhiteSpace(contaPadrao) ? null : contaPadrao.Trim();
        if (conta is not null && !Regex.IsMatch(conta, @"^(c|cc):\d+$"))
            throw new ArgumentException("Conta padrão inválida.");

        SemanaComecaNoDomingo = semanaComecaNoDomingo;
        ContaPadrao = conta;
        PeriodoRelatorios = periodoRelatorios;
        OcultarValores = ocultarValores;
    }

    public override string ToString() => $"[{Id}] Preferências de uso do membro {MembroId}";
}
