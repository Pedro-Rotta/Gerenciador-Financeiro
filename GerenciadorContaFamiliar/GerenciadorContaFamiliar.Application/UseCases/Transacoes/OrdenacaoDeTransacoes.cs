using System.Globalization;
using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Application.UseCases.Transacoes;

/// <summary>
/// Ordenação das setas da tabela de transações do Dashboard, para o PDF sair na mesma ordem
/// da tela. Mesmas regras de frontend/src/utils/ordenacaoTransacoes.ts: a coluna marcada
/// primeiro decide, as seguintes desempatam; no fim, data mais recente e a lançada por último.
/// Parâmetro: "conta,valor:inv" (":inv" = sentido contrário ao padrão da coluna).
/// </summary>
public static class OrdenacaoDeTransacoes
{
    public record Criterio(string Coluna, bool Invertido);

    private static readonly string[] Colunas = { "descricao", "categoria", "conta", "data", "valor" };

    // Alfabético sem diferenciar maiúsculas e acentos, como o localeCompare do front
    private static readonly StringComparer Texto =
        StringComparer.Create(CultureInfo.GetCultureInfo("pt-BR"), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);

    /// <summary>Lê o parâmetro (vazio = ordem padrão). Coluna desconhecida ou repetida → ArgumentException.</summary>
    public static IReadOnlyList<Criterio> Ler(string? parametro)
    {
        var criterios = new List<Criterio>();
        if (string.IsNullOrWhiteSpace(parametro))
            return criterios;

        foreach (var parte in parametro.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var invertido = parte.EndsWith(":inv", StringComparison.OrdinalIgnoreCase);
            var coluna = (invertido ? parte[..^4] : parte).ToLowerInvariant();
            if (!Colunas.Contains(coluna) || criterios.Any(c => c.Coluna == coluna))
                throw new ArgumentException($"Ordenação inválida: \"{parte}\".");
            criterios.Add(new Criterio(coluna, invertido));
        }
        return criterios;
    }

    /// <summary>Nova lista na ordem dos critérios (ordenação estável).</summary>
    public static List<Transacao> Ordenar(IEnumerable<Transacao> transacoes, IReadOnlyList<Criterio> criterios)
    {
        var comparador = Comparer<Transacao>.Create((a, b) =>
        {
            foreach (var criterio in criterios)
            {
                var r = Comparar(criterio.Coluna, a, b);
                if (r != 0)
                    return criterio.Invertido ? -r : r;
            }
            // Padrão da tabela: data mais recente, depois a lançada por último
            var porData = b.DataTransacao.CompareTo(a.DataTransacao);
            return porData != 0 ? porData : b.Id.CompareTo(a.Id);
        });
        return transacoes.OrderBy(t => t, comparador).ToList();
    }

    /// <summary>Ordem em texto para o cabeçalho do PDF.</summary>
    public static string Descrever(IReadOnlyList<Criterio> criterios)
    {
        if (criterios.Count == 0)
            return "Data (mais recente primeiro)";
        return string.Join("; ", criterios.Select(c => c.Coluna switch
        {
            "descricao" => c.Invertido ? "Descrição (Z-A)" : "Descrição (A-Z)",
            "categoria" => c.Invertido ? "Categoria (Z-A)" : "Categoria (A-Z)",
            "conta" => c.Invertido ? "Conta (inversa)" : "Conta (principal, depois A-Z)",
            "data" => c.Invertido ? "Data (mais antiga primeiro)" : "Data (mais recente primeiro)",
            _ => c.Invertido ? "Valor (menor primeiro)" : "Valor (maior primeiro)",
        }));
    }

    // Sentido padrão de cada coluna
    private static int Comparar(string coluna, Transacao a, Transacao b) => coluna switch
    {
        "descricao" => CompararTexto(a.Descricao, b.Descricao),
        "categoria" => CompararTexto(a.Categoria?.Nome, b.Categoria?.Nome),
        "conta" => CompararConta(a, b),
        "data" => b.DataTransacao.CompareTo(a.DataTransacao),
        _ => b.Valor.CompareTo(a.Valor),
    };

    // Vazio vai para o fim
    private static int CompararTexto(string? a, string? b)
    {
        var vazioA = string.IsNullOrWhiteSpace(a);
        var vazioB = string.IsNullOrWhiteSpace(b);
        if (vazioA || vazioB)
            return vazioA.CompareTo(vazioB);
        return Texto.Compare(a, b);
    }

    // Mesma ordem dos seletores: a principal, as individuais (A-Z) e depois as conjuntas (A-Z)
    private static int CompararConta(Transacao a, Transacao b)
    {
        static int Grupo(Transacao t) => t.Conta is not null ? (t.Conta.ContaPrincipal ? 0 : 1) : 2;
        var porGrupo = Grupo(a).CompareTo(Grupo(b));
        if (porGrupo != 0)
            return porGrupo;
        return CompararTexto(a.Conta?.Nome ?? a.ContaConjunta?.Nome, b.Conta?.Nome ?? b.ContaConjunta?.Nome);
    }
}
