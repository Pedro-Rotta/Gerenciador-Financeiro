/**
 * "Exportar meus dados" (Configurações → Privacidade): uma planilha .xlsx com tudo o que o
 * membro vê no app — Contas, Transações, Categorias e Metas —, uma aba para cada, como Tabela
 * do Excel (cabeçalho destacado, linhas zebradas e filtros), moeda em R$ e datas reais.
 * Montada no navegador a partir dos endpoints de sempre; o ExcelJS é carregado só ao exportar.
 */

import type { Workbook } from 'exceljs'
import { listarContas } from '../services/contas'
import { listarContasConjuntas } from '../services/contasConjuntas'
import { listarTransacoes } from '../services/transacoes'
import { listarTodasCategorias } from '../services/categorias'
import { listarMetas } from '../services/metas'
import type { TransacaoResponse } from '../types/transacao'
import { StatusTransacao, TipoTransacao } from '../types/transacao'
import { dataLocalISO } from './data'

const MOEDA = '"R$" #,##0.00;[Red]-"R$" #,##0.00'
const DATA = 'dd/mm/yyyy'
const ESTILO_TABELA = 'TableStyleMedium2'

type Celula = string | number | Date | null

interface Coluna {
  nome: string
  largura: number
  formato?: string
}

// Data "yyyy-MM-dd" como data do Excel (UTC, para não deslocar um dia pelo fuso)
function dataExcel(iso: string | null): Date | null {
  if (!iso) return null
  const [ano, mes, dia] = iso.slice(0, 10).split('-').map(Number)
  return new Date(Date.UTC(ano, mes - 1, dia))
}

function textoStatus(status: number): string {
  if (status === StatusTransacao.PENDENTE) return 'Pendente'
  if (status === StatusTransacao.ATRASADA) return 'Atrasada'
  return 'Concluída'
}

// Uma aba com título na linha 1 e a tabela a partir da linha 3 (ou um aviso, se vazia)
function aba(wb: Workbook, nome: string, titulo: string, colunas: Coluna[], linhas: Celula[][]) {
  const ws = wb.addWorksheet(nome)
  ws.getCell('A1').value = titulo
  ws.getCell('A1').font = { bold: true, size: 14 }
  ws.columns = colunas.map((c) => ({ width: c.largura }))

  if (linhas.length === 0) {
    ws.getCell('A3').value = 'Nenhum registro.'
    ws.getCell('A3').font = { italic: true, color: { argb: 'FF6B7280' } }
    return
  }

  ws.addTable({
    name: nome,
    ref: 'A3',
    headerRow: true,
    style: { theme: ESTILO_TABELA, showRowStripes: true },
    columns: colunas.map((c) => ({ name: c.nome, filterButton: true })),
    rows: linhas,
  })
  colunas.forEach((c, i) => {
    if (!c.formato) return
    for (let linha = 4; linha < 4 + linhas.length; linha++) ws.getCell(linha, i + 1).numFmt = c.formato
  })
  ws.views = [{ state: 'frozen', ySplit: 3 }]
}

/** Nome sugerido: "meus-dados-2026-10-14.xlsx". */
export function nomeArquivoMeusDados(): string {
  return `meus-dados-${dataLocalISO()}.xlsx`
}

/** Busca os dados do membro logado e monta a planilha. */
export async function gerarExcelMeusDados(membroId: number): Promise<Blob> {
  const [contas, conjuntas, categorias, metas] = await Promise.all([
    listarContas(),
    listarContasConjuntas(),
    listarTodasCategorias(),
    listarMetas(),
  ])

  // Transações de todas as contas (individuais e conjuntas), com o nome da conta
  const porConta = await Promise.all([
    ...contas.map(async (c) => ({ conta: c.nome, lista: await listarTransacoes({ contaId: c.id }) })),
    ...conjuntas.map(async (c) => ({
      conta: `${c.nome} (conjunta)`,
      lista: await listarTransacoes({ contaConjuntaId: c.id }),
    })),
  ])
  const nomeCategoria = new Map(categorias.map((c) => [c.id, c.nome]))
  const transacoes: { conta: string; t: TransacaoResponse }[] = porConta
    .flatMap(({ conta, lista }) => lista.map((t) => ({ conta, t })))
    .sort((a, b) => b.t.dataTransacao.localeCompare(a.t.dataTransacao))

  const nomeDaConta = (contaId: number | null, conjuntaId: number | null) =>
    contaId != null
      ? (contas.find((c) => c.id === contaId)?.nome ?? '—')
      : (conjuntas.find((c) => c.id === conjuntaId)?.nome ?? '—')

  const { default: ExcelJS } = await import('exceljs')
  const wb = new ExcelJS.Workbook()
  wb.creator = 'Gerenciador Familiar'
  wb.created = new Date()

  aba(
    wb,
    'Contas',
    'Contas',
    [
      { nome: 'Conta', largura: 30 },
      { nome: 'Tipo', largura: 14 },
      { nome: 'Saldo', largura: 18, formato: MOEDA },
      { nome: 'Principal', largura: 12 },
    ],
    [
      ...contas.map((c): Celula[] => [c.nome, 'Individual', c.saldo, c.contaPrincipal ? 'Sim' : 'Não']),
      ...conjuntas.map((c): Celula[] => [c.nome, 'Conjunta', c.saldo, 'Não']),
    ],
  )

  aba(
    wb,
    'Transacoes',
    'Transações',
    [
      { nome: 'Data', largura: 14, formato: DATA },
      { nome: 'Descrição', largura: 36 },
      { nome: 'Conta', largura: 28 },
      { nome: 'Categoria', largura: 22 },
      { nome: 'Tipo', largura: 10 },
      { nome: 'Valor', largura: 16, formato: MOEDA },
      { nome: 'Status', largura: 12 },
      { nome: 'Recorrente', largura: 12 },
      { nome: 'Lançada por você', largura: 16 },
    ],
    transacoes.map(({ conta, t }): Celula[] => [
      dataExcel(t.dataTransacao),
      t.descricao ?? '',
      conta,
      t.categoriaId != null ? (nomeCategoria.get(t.categoriaId) ?? '—') : '—',
      t.tipo === TipoTransacao.ENTRADA ? 'Entrada' : 'Saída',
      t.tipo === TipoTransacao.ENTRADA ? t.valor : -t.valor,
      textoStatus(t.status),
      t.recorrente ? 'Sim' : 'Não',
      t.membroId === membroId ? 'Sim' : 'Não',
    ]),
  )

  aba(
    wb,
    'Categorias',
    'Categorias',
    [
      { nome: 'Categoria', largura: 26 },
      { nome: 'Tipo', largura: 10 },
      { nome: 'Conta', largura: 28 },
      { nome: 'Subcategorias', largura: 50 },
    ],
    categorias.map((c): Celula[] => [
      c.nome,
      c.tipo === 0 ? 'Entrada' : 'Saída',
      nomeDaConta(c.contaId, c.contaConjuntaId),
      c.subCategorias.map((s) => s.nome).join(', '),
    ]),
  )

  aba(
    wb,
    'Metas',
    'Metas',
    [
      { nome: 'Meta', largura: 30 },
      { nome: 'Valor alvo', largura: 16, formato: MOEDA },
      { nome: 'Valor atual', largura: 16, formato: MOEDA },
      { nome: 'Prazo', largura: 14, formato: DATA },
      { nome: 'Concluída', largura: 12 },
    ],
    metas.map((m): Celula[] => [m.nome, m.valorAlvo, m.valorAtual, dataExcel(m.prazo), m.concluida ? 'Sim' : 'Não']),
  )

  const buffer = await wb.xlsx.writeBuffer()
  return new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
}
