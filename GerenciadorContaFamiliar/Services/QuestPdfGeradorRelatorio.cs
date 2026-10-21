using System.Text;
using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Relatorios;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static GerenciadorContaFamiliar.Services.PdfComum;

namespace GerenciadorContaFamiliar.Services;

/// <summary>
/// PDF do relatório financeiro com QuestPDF (A4 retrato, padrão de PdfComum):
/// cabeçalho com membro, período e data de geração; os 4 cards do resumo; gráficos
/// de evolução (receitas x despesas) e de distribuição das despesas por categoria
/// (SVG, mesmas cores da tela); tabelas completas de despesas e do comparativo
/// mensal, com cabeçalho repetido a cada página e rodapé "Página X de Y".
/// </summary>
public class QuestPdfGeradorRelatorio : IGeradorPdfRelatorio
{
    private static readonly string[] Meses = { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };

    // Mesmas cores do tema claro da tela (index.css: --grafico-* e --serie-*)
    private const string CorReceita = "#4f46e5";
    private const string CorDespesa = "#dc2626";
    private static readonly string[] CoresSerie = { "#2a78d6", "#eb6834", "#1baf7a", "#eda100", "#e87ba4", "#008300" };
    private const string CorOutros = "#898781";

    private const int MesesMinimosNoGrafico = 6;
    private const int MaxFatias = 6;

    public byte[] Gerar(RelatorioResumoResponse r, string nomeMembro)
    {
        var periodo = RotuloPeriodo(r);

        return Document.Create(doc => doc.Page(pagina =>
        {
            ConfigurarPagina(pagina, "Relatório Financeiro", $"{nomeMembro} · Período: {periodo} · {GeradoEm()}");

            pagina.Content().PaddingVertical(12).Column(col =>
            {
                col.Spacing(16);
                col.Item().Element(c => Cards(c, r));
                col.Item().Element(c => Evolucao(c, r));
                col.Item().Element(c => Distribuicao(c, r, periodo));
                col.Item().Element(c => TabelaDespesas(c, r));
                col.Item().Element(c => TabelaComparativo(c, r));
            });
        })).GeneratePdf();
    }

    /* ---------- Cards do resumo ---------- */

    private record ItemCard(string Rotulo, string Valor, string? Detalhe, bool? Positivo);

    private static void Cards(IContainer container, RelatorioResumoResponse r)
    {
        var nomePeriodo = r.Meses == 1 ? "mês" : "período";
        var cards = new List<ItemCard>();

        // 1. Média de gastos diários vs. período anterior de mesmo tamanho (gastar menos é positivo)
        var anterior = r.MediaGastosDiariosMesAnterior;
        if (anterior > 0)
        {
            var variacao = (r.MediaGastosDiarios - anterior) / anterior * 100;
            var detalhe = Math.Abs(variacao) < 0.05m
                ? $"igual ao {nomePeriodo} anterior"
                : $"{Percentual(Math.Abs(variacao))} {(variacao > 0 ? "acima" : "abaixo")} do {nomePeriodo} anterior";
            cards.Add(new ItemCard("Média de gastos diários", Moeda(r.MediaGastosDiarios), detalhe, variacao <= 0));
        }
        else
        {
            cards.Add(new ItemCard("Média de gastos diários", Moeda(r.MediaGastosDiarios), $"Sem gastos no {nomePeriodo} anterior", null));
        }

        // 2. Economia: % da receita que sobrou
        var rotuloEconomia = r.Meses == 1 ? "Economia mensal" : "Economia no período";
        cards.Add(r.EconomiaPercentual is decimal economia
            ? new ItemCard(rotuloEconomia, Percentual(economia), economia >= 0 ? "da receita poupada" : "gastou mais do que recebeu", economia >= 0)
            : new ItemCard(rotuloEconomia, "—", $"Sem receitas no {nomePeriodo}", null));

        // 3. Maior categoria de gastos
        cards.Add(new ItemCard("Maior categoria de gastos", r.MaiorCategoriaNome ?? "—",
            r.MaiorCategoriaNome is null ? "Nenhuma despesa no período" : $"{Moeda(r.MaiorCategoriaValor)} no período", null));

        // 4. Quanto guardar por mês para as metas vs. o que sobrou por mês
        if (r.MetasEmAndamento > 0)
        {
            var sobra = (r.ReceitasMes - r.DespesasMes) / r.Meses;
            var sufixo = r.Meses == 1 ? "no mês" : "por mês, em média";
            var detalhe = sobra < 0
                ? $"gastou {Moeda(-sobra)} a mais do que recebeu {sufixo}"
                : sobra >= r.AporteMensalMetas
                    ? $"sobraram {Moeda(sobra)} {sufixo}"
                    : $"sobraram {Moeda(sobra)} {sufixo}, faltam {Moeda(r.AporteMensalMetas - sobra)}";
            cards.Add(new ItemCard("Guardar por mês para metas", Moeda(r.AporteMensalMetas), detalhe, sobra >= r.AporteMensalMetas));
        }
        else
        {
            cards.Add(new ItemCard("Guardar por mês para metas", "—", "Nenhuma meta em andamento", null));
        }

        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
            });
            foreach (var card in cards)
            {
                // Sem setas ▲▼: a fonte embutida (Lato) não tem esses símbolos; o texto
                // ("acima/abaixo", "faltam") já diz a direção e a cor reforça
                var cor = card.Positivo switch { true => Sucesso, false => Perigo, null => TextoSuave };
                t.Cell().Padding(4).Element(c => Card(c, card.Rotulo, card.Valor, card.Detalhe, cor));
            }
        });
    }

    /* ---------- Gráfico de evolução (SVG) ---------- */

    private static void Evolucao(IContainer container, RelatorioResumoResponse r)
    {
        var quantidade = Math.Max(MesesMinimosNoGrafico, r.Meses);
        var dados = r.Comparativo.TakeLast(quantidade).ToList();

        container.Column(col =>
        {
            Titulo(col, "Evolução temporal", $"Receitas vs. despesas nos últimos {quantidade} meses");
            col.Item().PaddingBottom(4).Row(row =>
            {
                row.AutoItem().Element(c => ChaveLinha(c, CorReceita, "Receitas"));
                row.ConstantItem(14);
                row.AutoItem().Element(c => ChaveLinha(c, CorDespesa, "Despesas"));
            });

            if (dados.Count == 0 || dados.All(m => m.Receita == 0 && m.Despesa == 0))
            {
                col.Item().Text("Nenhuma movimentação nos últimos meses.").FontColor(TextoSuave);
                return;
            }
            col.Item().Height(190).Svg(SvgEvolucao(dados));
        });
    }

    private static string SvgEvolucao(List<RelatorioMesItem> dados)
    {
        const double largura = 523, altura = 190, padEsq = 56, padDir = 16, padTop = 14, padBase = 26;
        var maior = (double)dados.Max(m => Math.Max(m.Receita, m.Despesa));
        var passo = NumeroRedondo(maior / 4);
        var topo = passo * 4;
        var util = largura - padEsq - padDir;
        double Px(int i) => dados.Count == 1 ? padEsq + util / 2 : padEsq + i * util / (dados.Count - 1);
        double Py(double v) => padTop + (1 - v / topo) * (altura - padTop - padBase);

        var svg = new StringBuilder();
        svg.Append(FormattableString.Invariant($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {largura} {altura}\" font-family=\"Arial, Helvetica, sans-serif\">"));

        // Grade (hairline contínua) e eixo Y
        for (var n = 0; n <= 4; n++)
        {
            var y = Py(passo * n);
            var cor = n == 0 ? "#d6d9e3" : Borda;
            svg.Append(FormattableString.Invariant($"<line x1=\"{padEsq}\" x2=\"{largura - padDir}\" y1=\"{y:0.##}\" y2=\"{y:0.##}\" stroke=\"{cor}\" stroke-width=\"1\"/>"));
            svg.Append(FormattableString.Invariant($"<text x=\"{padEsq - 6}\" y=\"{y + 3:0.##}\" font-size=\"8\" fill=\"#9ca3af\" text-anchor=\"end\">{Esc(n == 0 ? "R$ 0" : MoedaCompacta((decimal)(passo * n)))}</text>"));
        }

        // Meses
        for (var i = 0; i < dados.Count; i++)
            svg.Append(FormattableString.Invariant($"<text x=\"{Px(i):0.##}\" y=\"{altura - 8}\" font-size=\"8\" fill=\"#9ca3af\" text-anchor=\"middle\">{Esc(RotuloMes(dados[i].Ano, dados[i].Mes))}</text>"));

        // Linhas e marcadores (anel branco)
        foreach (var (cor, valor) in new (string, Func<RelatorioMesItem, decimal>)[] { (CorReceita, m => m.Receita), (CorDespesa, m => m.Despesa) })
        {
            var pontos = string.Join(" ", dados.Select((m, i) => FormattableString.Invariant($"{Px(i):0.##},{Py((double)valor(m)):0.##}")));
            svg.Append($"<polyline points=\"{pontos}\" fill=\"none\" stroke=\"{cor}\" stroke-width=\"2\" stroke-linejoin=\"round\" stroke-linecap=\"round\"/>");
            for (var i = 0; i < dados.Count; i++)
                svg.Append(FormattableString.Invariant($"<circle cx=\"{Px(i):0.##}\" cy=\"{Py((double)valor(dados[i])):0.##}\" r=\"3.5\" fill=\"{cor}\" stroke=\"#ffffff\" stroke-width=\"1.5\"/>"));
        }

        // Rótulos diretos do último mês: a série mais alta acima do ponto, a outra abaixo
        var ultimo = dados[^1];
        var ancora = dados.Count == 1 ? "middle" : "end";
        var receitaAcima = ultimo.Receita >= ultimo.Despesa;
        foreach (var (valor, acima) in new[] { (ultimo.Receita, receitaAcima), (ultimo.Despesa, !receitaAcima) })
        {
            var y = Py((double)valor) + (acima ? -8 : 14);
            var rotulo = Esc(MoedaCompacta(valor));
            // Contorno branco e texto como dois elementos (o SVG do PDF não suporta paint-order):
            // a linha que cruza o rótulo fica por trás dele
            svg.Append(FormattableString.Invariant($"<text x=\"{Px(dados.Count - 1):0.##}\" y=\"{y:0.##}\" font-size=\"8.5\" font-weight=\"bold\" fill=\"#ffffff\" stroke=\"#ffffff\" stroke-width=\"3\" stroke-linejoin=\"round\" text-anchor=\"{ancora}\">{rotulo}</text>"));
            svg.Append(FormattableString.Invariant($"<text x=\"{Px(dados.Count - 1):0.##}\" y=\"{y:0.##}\" font-size=\"8.5\" font-weight=\"bold\" fill=\"#6b7280\" text-anchor=\"{ancora}\">{rotulo}</text>"));
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    /* ---------- Distribuição das despesas (rosca SVG + legenda) ---------- */

    private record Fatia(string Nome, decimal Valor, string Cor);

    private static void Distribuicao(IContainer container, RelatorioResumoResponse r, string periodo)
    {
        var fatias = MontarFatias(r.Despesas);
        var total = fatias.Sum(f => f.Valor);

        container.Column(col =>
        {
            Titulo(col, "Distribuição de despesas", $"Por categoria em {periodo}");
            if (total <= 0)
            {
                col.Item().Text("Nenhuma despesa no período.").FontColor(TextoSuave);
                return;
            }

            col.Item().Row(row =>
            {
                row.ConstantItem(150).Height(150).Layers(camadas =>
                {
                    camadas.PrimaryLayer().Svg(SvgRosca(fatias, total));
                    camadas.Layer().AlignCenter().AlignMiddle().Column(centro =>
                    {
                        centro.Item().AlignCenter().Text(Moeda(total)).FontSize(10).Bold();
                        centro.Item().AlignCenter().Text("Total de despesas").FontSize(7).FontColor(TextoSuave);
                    });
                });

                row.ConstantItem(20);
                row.RelativeItem().AlignMiddle().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(14);
                        c.RelativeColumn();
                        c.ConstantColumn(40);
                        c.ConstantColumn(80);
                    });
                    foreach (var f in fatias)
                    {
                        t.Cell().PaddingVertical(3).AlignMiddle().Width(9).Height(9).Background(f.Cor);
                        t.Cell().PaddingVertical(3).Text(f.Nome);
                        t.Cell().PaddingVertical(3).AlignRight().Text(Percentual(f.Valor / total * 100, 0)).FontColor(TextoSuave);
                        t.Cell().PaddingVertical(3).AlignRight().Text(Moeda(f.Valor)).Bold();
                    }
                });
            });
        });
    }

    // Soma por categoria; acima de MaxFatias, as menores viram "Outros". Mesma regra da tela.
    private static List<Fatia> MontarFatias(IEnumerable<RelatorioDespesaItem> despesas)
    {
        var ordenadas = despesas
            .GroupBy(d => d.Categoria)
            .Select(g => (Nome: g.Key, Valor: g.Sum(d => d.Valor)))
            .OrderByDescending(g => g.Valor)
            .ToList();

        var principais = ordenadas.Count > MaxFatias ? ordenadas.Take(MaxFatias - 1).ToList() : ordenadas;
        var resto = ordenadas.Skip(principais.Count).Sum(g => g.Valor);

        var cores = CoresPorCategoria(principais.Select(p => p.Nome));
        var fatias = principais.Select(p => new Fatia(p.Nome, p.Valor, cores[p.Nome])).ToList();
        if (resto > 0) fatias.Add(new Fatia("Outros", resto, CorOutros));
        return fatias;
    }

    // Cor estável pelo nome (mesmo cálculo da tela): a categoria mantém a cor entre meses
    private static Dictionary<string, string> CoresPorCategoria(IEnumerable<string> nomes)
    {
        var usados = new HashSet<int>();
        var cores = new Dictionary<string, string>();
        foreach (var nome in nomes)
        {
            var acumulado = 7;
            foreach (var c in nome) acumulado = (acumulado * 31 + c) % 9973;
            var slot = acumulado % CoresSerie.Length;
            while (usados.Contains(slot)) slot = (slot + 1) % CoresSerie.Length;
            usados.Add(slot);
            cores[nome] = CoresSerie[slot];
        }
        return cores;
    }

    // Rosca com fatias em arco e 2pt de espaço branco entre elas
    private static string SvgRosca(List<Fatia> fatias, decimal total)
    {
        const double cx = 75, cy = 75, externo = 72, interno = 50;
        var svg = new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 150 150\">");

        if (fatias.Count == 1)
        {
            var raio = (externo + interno) / 2;
            svg.Append(FormattableString.Invariant($"<circle cx=\"{cx}\" cy=\"{cy}\" r=\"{raio}\" fill=\"none\" stroke=\"{fatias[0].Cor}\" stroke-width=\"{externo - interno}\"/>"));
        }
        else
        {
            var espaco = 2.0 / externo; // ângulo equivalente a ~2pt na borda externa
            var inicio = -Math.PI / 2;
            foreach (var f in fatias)
            {
                var angulo = (double)(f.Valor / total) * 2 * Math.PI;
                var a0 = inicio + espaco / 2;
                var a1 = inicio + Math.Max(angulo - espaco / 2, espaco / 2 + 0.001);
                inicio += angulo;
                var grande = a1 - a0 > Math.PI ? 1 : 0;
                double X(double raio, double a) => cx + raio * Math.Cos(a);
                double Y(double raio, double a) => cy + raio * Math.Sin(a);
                svg.Append(FormattableString.Invariant(
                    $"<path d=\"M {X(externo, a0):0.###} {Y(externo, a0):0.###} A {externo} {externo} 0 {grande} 1 {X(externo, a1):0.###} {Y(externo, a1):0.###} "));
                svg.Append(FormattableString.Invariant(
                    $"L {X(interno, a1):0.###} {Y(interno, a1):0.###} A {interno} {interno} 0 {grande} 0 {X(interno, a0):0.###} {Y(interno, a0):0.###} Z\" fill=\"{f.Cor}\"/>"));
            }
        }

        svg.Append("</svg>");
        return svg.ToString();
    }

    /* ---------- Tabelas ---------- */

    private static void TabelaDespesas(IContainer container, RelatorioResumoResponse r)
    {
        var despesas = r.Despesas.OrderBy(d => d.DataTransacao).ToList();
        container.Column(col =>
        {
            Titulo(col, "Despesas do período", $"{despesas.Count} {(despesas.Count == 1 ? "despesa" : "despesas")}, em ordem de data");
            if (despesas.Count == 0)
            {
                col.Item().Text("Nenhuma despesa no período.").FontColor(TextoSuave);
                return;
            }

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(62);
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                    c.ConstantColumn(80);
                });
                t.Header(h =>
                {
                    CelulaCabecalho(h.Cell(), "Data");
                    CelulaCabecalho(h.Cell(), "Descrição");
                    CelulaCabecalho(h.Cell(), "Categoria");
                    CelulaCabecalho(h.Cell(), "Valor", direita: true);
                });
                for (var i = 0; i < despesas.Count; i++)
                {
                    var d = despesas[i];
                    var zebra = i % 2 == 1;
                    Celula(t.Cell(), d.DataTransacao.ToString("dd/MM/yyyy", PtBr), zebra);
                    Celula(t.Cell(), d.Descricao, zebra);
                    Celula(t.Cell(), d.Categoria, zebra);
                    Celula(t.Cell(), Moeda(d.Valor), zebra, direita: true);
                }
                CelulaTotal(t.Cell().ColumnSpan(3), "Total");
                CelulaTotal(t.Cell(), Moeda(despesas.Sum(d => d.Valor)), direita: true);
            });
        });
    }

    private static void TabelaComparativo(IContainer container, RelatorioResumoResponse r)
    {
        container.Column(col =>
        {
            Titulo(col, "Comparativo mensal", "Receitas, despesas e resultado de cada mês");
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn();
                });
                t.Header(h =>
                {
                    CelulaCabecalho(h.Cell(), "Mês");
                    CelulaCabecalho(h.Cell(), "Receita", direita: true);
                    CelulaCabecalho(h.Cell(), "Despesa", direita: true);
                    CelulaCabecalho(h.Cell(), "Resultado", direita: true);
                    CelulaCabecalho(h.Cell(), "Variação", direita: true);
                });
                for (var i = 0; i < r.Comparativo.Count; i++)
                {
                    var m = r.Comparativo[i];
                    var zebra = i % 2 == 1;
                    Celula(t.Cell(), RotuloMes(m.Ano, m.Mes), zebra);
                    Celula(t.Cell(), Moeda(m.Receita), zebra, direita: true);
                    Celula(t.Cell(), Moeda(m.Despesa), zebra, direita: true);
                    Celula(t.Cell(), Moeda(m.Resultado), zebra, direita: true, cor: m.Resultado < 0 ? Perigo : null);
                    Celula(t.Cell(), m.VariacaoPercentual is decimal v ? $"{(v > 0 ? "+" : "")}{Percentual(v)}" : "—", zebra, direita: true);
                }
                CelulaTotal(t.Cell(), "Total");
                CelulaTotal(t.Cell(), Moeda(r.Comparativo.Sum(m => m.Receita)), direita: true);
                CelulaTotal(t.Cell(), Moeda(r.Comparativo.Sum(m => m.Despesa)), direita: true);
                CelulaTotal(t.Cell(), Moeda(r.Comparativo.Sum(m => m.Resultado)), direita: true);
                CelulaTotal(t.Cell(), "");
            });
        });
    }

    /* ---------- Helpers ---------- */

    private static void ChaveLinha(IContainer c, string cor, string rotulo)
    {
        c.Row(row =>
        {
            row.AutoItem().AlignMiddle().Width(14).Height(2).Background(cor);
            row.AutoItem().PaddingLeft(4).Text(rotulo).FontSize(8).FontColor(TextoSuave);
        });
    }

    private static string RotuloPeriodo(RelatorioResumoResponse r)
    {
        if (r.Meses == 1) return RotuloMes(r.Ano, r.Mes);
        return $"{RotuloMes(r.DataInicio.Year, r.DataInicio.Month)} – {RotuloMes(r.Ano, r.Mes)}";
    }

    private static string RotuloMes(int ano, int mes) => $"{Meses[mes - 1]}/{ano % 100:00}";

    // "R$ 9,1 mil" para eixos e rótulos do gráfico
    private static string MoedaCompacta(decimal valor) =>
        Math.Abs(valor) >= 1000
            ? $"R$ {(valor / 1000).ToString("0.#", PtBr)} mil"
            : $"R$ {valor.ToString("0", PtBr)}";

    // Arredonda para cima até um número "redondo" (1, 2, 2,5, 5 ou 10 × 10^n)
    private static double NumeroRedondo(double valor)
    {
        if (valor <= 0) return 1;
        var potencia = Math.Pow(10, Math.Floor(Math.Log10(valor)));
        var fracao = valor / potencia;
        var redondo = fracao <= 1 ? 1 : fracao <= 2 ? 2 : fracao <= 2.5 ? 2.5 : fracao <= 5 ? 5 : 10;
        return redondo * potencia;
    }

    private static string Esc(string texto) =>
        texto.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
