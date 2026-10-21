using GerenciadorContaFamiliar.Application.DTOs.Responses;
using GerenciadorContaFamiliar.Application.UseCases.Interfaces.Transacoes;
using GerenciadorContaFamiliar.Domain;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using static GerenciadorContaFamiliar.Services.PdfComum;

namespace GerenciadorContaFamiliar.Services;

/// <summary>
/// PDF de transações com QuestPDF, no mesmo padrão do PDF dos Relatórios (PdfComum):
/// cabeçalho com membro e data de geração, os filtros aplicados, cards com entradas,
/// saídas, saldo e quantidade, e a tabela completa (cabeçalho repetido a cada página,
/// zebrado, entradas em verde e saídas em vermelho, saldo no total).
/// </summary>
public class QuestPdfGeradorTransacoes : IGeradorPdfTransacoes
{
    public byte[] Gerar(TransacoesPdfDados dados)
    {
        return Document.Create(doc => doc.Page(pagina =>
        {
            ConfigurarPagina(pagina, "Transações", $"{dados.NomeMembro} · {GeradoEm()}");

            pagina.Content().PaddingVertical(12).Column(col =>
            {
                col.Spacing(16);
                col.Item().Element(c => Filtros(c, dados));
                col.Item().Element(c => Cards(c, dados));
                col.Item().Element(c => Tabela(c, dados));
            });
        })).GeneratePdf();
    }

    // Filtros aplicados, em duas colunas, num quadro discreto
    private static void Filtros(IContainer container, TransacoesPdfDados dados)
    {
        container.Background(FundoSuave).Padding(10).Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
            });
            foreach (var (rotulo, valor) in dados.Filtros)
            {
                t.Cell().PaddingVertical(2).Text(texto =>
                {
                    texto.Span($"{rotulo}: ").FontColor(TextoSuave);
                    texto.Span(valor).Bold();
                });
            }
        });
    }

    private static void Cards(IContainer container, TransacoesPdfDados dados)
    {
        var entradas = dados.Itens.Where(i => i.Tipo == Transacao.TipoTransacao.ENTRADA).ToList();
        var saidas = dados.Itens.Where(i => i.Tipo == Transacao.TipoTransacao.SAIDA).ToList();
        var totalEntradas = entradas.Sum(i => i.Valor);
        var totalSaidas = saidas.Sum(i => i.Valor);
        var saldo = totalEntradas - totalSaidas;
        var pendentes = dados.Itens.Count(i => i.Status != Transacao.StatusTransacao.CONCLUIDA);

        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.RelativeColumn();
            });
            t.Cell().Padding(4).Element(c => Card(c, "Entradas", Moeda(totalEntradas), Quantidade(entradas.Count), Sucesso));
            t.Cell().Padding(4).Element(c => Card(c, "Saídas", Moeda(totalSaidas), Quantidade(saidas.Count), Perigo));
            t.Cell().Padding(4).Element(c => Card(c, "Saldo do período", Moeda(saldo),
                saldo >= 0 ? "entradas maiores que as saídas" : "saídas maiores que as entradas", saldo >= 0 ? Sucesso : Perigo));
            t.Cell().Padding(4).Element(c => Card(c, "Transações", dados.Itens.Count.ToString(PtBr),
                pendentes == 0 ? "todas concluídas" : $"{pendentes} pendente(s) ou atrasada(s)", pendentes == 0 ? TextoSuave : Perigo));
        });
    }

    private static void Tabela(IContainer container, TransacoesPdfDados dados)
    {
        container.Column(col =>
        {
            Titulo(col, "Transações", $"{Quantidade(dados.Itens.Count)}, da mais recente para a mais antiga");
            if (dados.Itens.Count == 0)
            {
                col.Item().Text("Nenhuma transação para os filtros escolhidos.").FontColor(TextoSuave);
                return;
            }

            col.Item().Table(t =>
            {
                t.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(62);
                    c.RelativeColumn(3);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.ConstantColumn(84);
                });
                t.Header(h =>
                {
                    CelulaCabecalho(h.Cell(), "Data");
                    CelulaCabecalho(h.Cell(), "Descrição");
                    CelulaCabecalho(h.Cell(), "Categoria");
                    CelulaCabecalho(h.Cell(), "Conta");
                    CelulaCabecalho(h.Cell(), "Valor", direita: true);
                });
                for (var i = 0; i < dados.Itens.Count; i++)
                {
                    var item = dados.Itens[i];
                    var zebra = i % 2 == 1;
                    var entrada = item.Tipo == Transacao.TipoTransacao.ENTRADA;
                    var descricao = item.Status switch
                    {
                        Transacao.StatusTransacao.PENDENTE => $"{item.Descricao} (pendente)",
                        Transacao.StatusTransacao.ATRASADA => $"{item.Descricao} (atrasada)",
                        _ => item.Descricao,
                    };
                    Celula(t.Cell(), item.DataTransacao.ToString("dd/MM/yyyy", PtBr), zebra);
                    Celula(t.Cell(), descricao, zebra);
                    Celula(t.Cell(), item.Categoria, zebra);
                    Celula(t.Cell(), item.Conta, zebra);
                    Celula(t.Cell(), $"{(entrada ? "+" : "-")} {Moeda(item.Valor)}", zebra, direita: true, cor: entrada ? Sucesso : Perigo);
                }

                var saldo = dados.Itens.Sum(i => i.Tipo == Transacao.TipoTransacao.ENTRADA ? i.Valor : -i.Valor);
                CelulaTotal(t.Cell().ColumnSpan(4), "Saldo (entradas - saídas)");
                CelulaTotal(t.Cell(), Moeda(saldo), direita: true, cor: saldo >= 0 ? Sucesso : Perigo);
            });
        });
    }

    private static string Quantidade(int n) => n == 1 ? "1 transação" : $"{n} transações";
}
