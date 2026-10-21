using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GerenciadorContaFamiliar.Services;

/// <summary>
/// Padrão visual dos PDFs (Relatórios e Transações): página A4 com cabeçalho
/// (título + linha de contexto) e rodapé "Página X de Y", cores do tema claro da
/// tela, títulos de seção, células de tabela (cabeçalho, zebrado, total) e formatação pt-BR.
/// </summary>
internal static class PdfComum
{
    public static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    // Cores do tema claro da tela (index.css)
    /// <summary>Fonte embutida (registrada no Program.cs) para os símbolos que a Lato não tem.</summary>
    public const string FonteDeReserva = "DejaVu Sans";

    public const string Texto = "#1f2937";
    public const string TextoSuave = "#6b7280";
    public const string Borda = "#e6e8ef";
    public const string BordaForte = "#d6d9e3";
    public const string FundoSuave = "#f6f7fb";
    public const string FundoCabecalho = "#eef2ff";
    public const string Primario = "#4f46e5";
    public const string Sucesso = "#16a34a";
    public const string Perigo = "#dc2626";

    /// <summary>A4 retrato, margens, fonte padrão, cabeçalho e rodapé em todas as páginas.</summary>
    public static void ConfigurarPagina(PageDescriptor pagina, string titulo, string linhaContexto)
    {
        pagina.Size(PageSizes.A4);
        pagina.Margin(36);
        pagina.PageColor(Colors.White);
        // Lato no texto; DejaVu Sans só nos caracteres que a Lato não tem (▲ ▼ ✓ ...)
        pagina.DefaultTextStyle(t => t.FontFamily("Lato", FonteDeReserva).FontSize(9).FontColor(Texto));

        pagina.Header().Column(col =>
        {
            col.Item().Text(titulo).FontSize(18).Bold();
            col.Item().Text(linhaContexto).FontSize(9).FontColor(TextoSuave);
            col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Borda);
        });

        pagina.Footer().Row(row =>
        {
            row.RelativeItem().Text("Finança · Gerenciador Familiar").FontSize(8).FontColor(TextoSuave);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(TextoSuave));
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });
    }

    /// <summary>"Gerado em 07/10/2026 23:34"</summary>
    public static string GeradoEm() => $"Gerado em {DateTime.Now.ToString("dd/MM/yyyy HH:mm", PtBr)}";

    public static void Titulo(ColumnDescriptor col, string titulo, string sub)
    {
        col.Item().Text(titulo).FontSize(12).Bold();
        col.Item().PaddingBottom(6).Text(sub).FontSize(8).FontColor(TextoSuave);
    }

    /// <summary>Card do resumo (rótulo, valor em destaque e detalhe opcional colorido).</summary>
    public static void Card(IContainer c, string rotulo, string valor, string? detalhe = null, string? corDetalhe = null)
    {
        c.Border(1).BorderColor(Borda).Background(Colors.White).Padding(10).Column(col =>
        {
            col.Item().Text(rotulo.ToUpper(PtBr)).FontSize(7).Bold().FontColor(TextoSuave);
            col.Item().PaddingTop(2).Text(valor).FontSize(15).Bold();
            if (detalhe is not null)
                col.Item().PaddingTop(2).Text(detalhe).FontSize(8).FontColor(corDetalhe ?? TextoSuave);
        });
    }

    public static void CelulaCabecalho(IContainer c, string texto, bool direita = false)
    {
        var celula = c.Background(FundoCabecalho).BorderBottom(1).BorderColor(Borda).PaddingVertical(5).PaddingHorizontal(6);
        (direita ? celula.AlignRight() : celula).Text(texto).Bold().FontColor(Primario);
    }

    public static void Celula(IContainer c, string texto, bool zebra, bool direita = false, string? cor = null)
    {
        var celula = c.Background(zebra ? FundoSuave : Colors.White).PaddingVertical(4).PaddingHorizontal(6);
        var bloco = (direita ? celula.AlignRight() : celula).Text(texto);
        if (cor is not null) bloco.FontColor(cor);
    }

    public static void CelulaTotal(IContainer c, string texto, bool direita = false, string? cor = null)
    {
        var celula = c.BorderTop(1).BorderColor(BordaForte).PaddingVertical(5).PaddingHorizontal(6);
        var bloco = (direita ? celula.AlignRight() : celula).Text(texto).Bold();
        if (cor is not null) bloco.FontColor(cor);
    }

    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    public static string Percentual(decimal valor, int casas = 1) =>
        valor.ToString(casas == 0 ? "0" : "0.#", PtBr) + "%";
}
