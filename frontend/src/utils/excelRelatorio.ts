/**
 * Exportação Excel (.xlsx) dos Relatórios, já formatada: três abas (Resumo,
 * Despesas e Comparativo), cada uma como Tabela do Excel (cabeçalho destacado,
 * linhas zebradas e filtros), com moeda em R$, percentuais, datas reais,
 * colunas ajustadas, cabeçalho congelado e totais por fórmula.
 * O ExcelJS é carregado só ao exportar (import dinâmico), sem pesar na página.
 */

import type { RelatorioResumoResponse } from '../types/relatorio'
import type { Workbook, Worksheet } from 'exceljs'

const MOEDA = '"R$" #,##0.00;[Red]-"R$" #,##0.00'
const PERCENTUAL = '0.0%'
const DATA = 'dd/mm/yyyy'
const MES = 'mm/yyyy'
const ESTILO_TABELA = 'TableStyleMedium2'

// Percentual da API (ex.: 35.2) como fração do Excel (0,352), sem resíduo de ponto flutuante
function fracao(percentual: number | null): number | null {
  return percentual === null ? null : Math.round(percentual * 10) / 1000
}

// Data "yyyy-MM-dd" como data do Excel (UTC, para não deslocar um dia pelo fuso)
function dataExcel(iso: string): Date {
  const [ano, mes, dia] = iso.split('-').map(Number)
  return new Date(Date.UTC(ano, mes - 1, dia))
}

// Primeiro mês do período ("yyyy-MM"), a partir do último mês e da quantidade de meses
function inicioPeriodo(r: RelatorioResumoResponse): string {
  const inicio = new Date(r.ano, r.mes - r.meses, 1)
  return `${inicio.getFullYear()}-${String(inicio.getMonth() + 1).padStart(2, '0')}`
}

/** Nome sugerido: "relatorio-2026-10.xlsx" ou "relatorio-2026-08-a-2026-10.xlsx". */
export function nomeArquivoExcel(r: RelatorioResumoResponse): string {
  const fim = `${r.ano}-${String(r.mes).padStart(2, '0')}`
  return r.meses === 1 ? `relatorio-${fim}.xlsx` : `relatorio-${inicioPeriodo(r)}-a-${fim}.xlsx`
}

// Título e subtítulo no topo da aba (linhas 1 e 2); a tabela começa na linha 4
function cabecalhoDaAba(ws: Worksheet, titulo: string, subtitulo: string) {
  ws.getCell('A1').value = titulo
  ws.getCell('A1').font = { bold: true, size: 14 }
  ws.getCell('A2').value = subtitulo
  ws.getCell('A2').font = { italic: true, color: { argb: 'FF6B7280' } }
}

// Congela o cabeçalho da tabela (linha 4) ao rolar
function congelarAteLinha(ws: Worksheet, linha: number) {
  ws.views = [{ state: 'frozen', ySplit: linha }]
}

function abaResumo(wb: Workbook, r: RelatorioResumoResponse, subtitulo: string) {
  const ws = wb.addWorksheet('Resumo')
  cabecalhoDaAba(ws, 'Resumo', subtitulo)
  ws.columns = [{ width: 40 }, { width: 22 }]

  // [indicador, valor, formato]
  const linhas: [string, number | string | null, string | undefined][] = [
    ['Receitas', r.receitasMes, MOEDA],
    ['Despesas', r.despesasMes, MOEDA],
    ['Resultado', r.receitasMes - r.despesasMes, MOEDA],
    ['Economia', fracao(r.economiaPercentual), PERCENTUAL],
    ['Média de gastos diários', r.mediaGastosDiarios, MOEDA],
    ['Média diária do período anterior', r.mediaGastosDiariosMesAnterior, MOEDA],
    ['Maior categoria de gastos', r.maiorCategoriaNome ?? '—', undefined],
    ['Valor da maior categoria', r.maiorCategoriaValor, MOEDA],
    ['Guardar por mês para metas', r.aporteMensalMetas ?? null, MOEDA],
    ['Metas em andamento', r.metasEmAndamento ?? null, '0'],
  ]

  ws.addTable({
    name: 'Resumo',
    ref: 'A4',
    headerRow: true,
    style: { theme: ESTILO_TABELA, showRowStripes: true },
    columns: [{ name: 'Indicador', filterButton: false }, { name: 'Valor', filterButton: false }],
    rows: linhas.map(([indicador, valor]) => [indicador, valor]),
  })
  // Formato de cada valor (a tabela começa na linha 4; dados a partir da 5)
  linhas.forEach(([, , formato], i) => {
    const celula = ws.getCell(5 + i, 2)
    if (formato) celula.numFmt = formato
    celula.alignment = { horizontal: 'right' }
  })
  congelarAteLinha(ws, 4)
}

function abaDespesas(wb: Workbook, r: RelatorioResumoResponse, subtitulo: string) {
  const ws = wb.addWorksheet('Despesas')
  cabecalhoDaAba(ws, 'Despesas do período', subtitulo)
  ws.columns = [{ width: 14 }, { width: 42 }, { width: 22 }, { width: 18 }]

  const despesas = [...r.despesas].sort((a, b) => a.dataTransacao.localeCompare(b.dataTransacao))
  ws.addTable({
    name: 'Despesas',
    ref: 'A4',
    headerRow: true,
    totalsRow: true,
    style: { theme: ESTILO_TABELA, showRowStripes: true },
    columns: [
      { name: 'Data', filterButton: true, totalsRowLabel: 'Total' },
      { name: 'Descrição', filterButton: true },
      { name: 'Categoria', filterButton: true },
      { name: 'Valor', filterButton: true, totalsRowFunction: 'sum' },
    ],
    // Tabela vazia não é válida no Excel: deixa uma linha indicando que não há despesas
    rows: despesas.length
      ? despesas.map((d) => [dataExcel(d.dataTransacao), d.descricao, d.categoria, d.valor])
      : [[null, 'Nenhuma despesa no período', null, 0]],
  })
  ws.getColumn(1).numFmt = DATA
  ws.getColumn(4).numFmt = MOEDA
  congelarAteLinha(ws, 4)
}

function abaComparativo(wb: Workbook, r: RelatorioResumoResponse, subtitulo: string) {
  const ws = wb.addWorksheet('Comparativo')
  cabecalhoDaAba(ws, 'Comparativo mensal', subtitulo)
  ws.columns = [{ width: 12 }, { width: 18 }, { width: 18 }, { width: 18 }, { width: 14 }]

  ws.addTable({
    name: 'Comparativo',
    ref: 'A4',
    headerRow: true,
    totalsRow: true,
    style: { theme: ESTILO_TABELA, showRowStripes: true },
    columns: [
      { name: 'Mês', filterButton: true, totalsRowLabel: 'Total' },
      { name: 'Receita', filterButton: true, totalsRowFunction: 'sum' },
      { name: 'Despesa', filterButton: true, totalsRowFunction: 'sum' },
      { name: 'Resultado', filterButton: true, totalsRowFunction: 'sum' },
      { name: 'Variação', filterButton: true },
    ],
    rows: r.comparativo.map((m) => [
      new Date(Date.UTC(m.ano, m.mes - 1, 1)),
      m.receita,
      m.despesa,
      m.resultado,
      fracao(m.variacaoPercentual),
    ]),
  })
  ws.getColumn(1).numFmt = MES
  ws.getColumn(2).numFmt = MOEDA
  ws.getColumn(3).numFmt = MOEDA
  ws.getColumn(4).numFmt = MOEDA
  ws.getColumn(5).numFmt = PERCENTUAL
  congelarAteLinha(ws, 4)
}

/** Gera o .xlsx do resumo carregado; `periodo` é o rótulo exibido na tela (ex.: "Jul/26 – Set/26"). */
export async function gerarExcelRelatorio(r: RelatorioResumoResponse, periodo: string): Promise<Blob> {
  const { default: ExcelJS } = await import('exceljs')
  const wb = new ExcelJS.Workbook()
  wb.creator = 'Finança · Gerenciador Familiar'
  wb.created = new Date()

  const gerado = new Date().toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
  const subtitulo = `Período: ${periodo} · Gerado em ${gerado}`

  abaResumo(wb, r, subtitulo)
  abaDespesas(wb, r, subtitulo)
  abaComparativo(wb, r, subtitulo)

  const buffer = await wb.xlsx.writeBuffer()
  return new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
}
