/**
 * Ordenação da tabela de transações do Dashboard pelas setas dos títulos.
 * Cada coluna tem 3 estados ao clicar: sentido padrão → invertido → desmarcada.
 * Várias colunas marcadas viram uma só ordenação (Array.sort, O(n log n)): a marcada
 * primeiro decide; as seguintes só desempatam. Mesma regra do PDF (parâmetro `ordenacao`).
 */

import type { TransacaoResponse } from '../types/transacao'

export type ColunaOrdenacao = 'descricao' | 'categoria' | 'conta' | 'data' | 'valor'

export interface CriterioOrdenacao {
  coluna: ColunaOrdenacao
  /** Sentido contrário ao padrão da coluna */
  invertido: boolean
}

/** Sentido padrão de cada coluna (o 1º clique), em texto para a dica da seta */
export const SENTIDO_PADRAO: Record<ColunaOrdenacao, string> = {
  descricao: 'A → Z',
  categoria: 'A → Z',
  conta: 'principal, depois A → Z',
  data: 'mais recente primeiro',
  valor: 'maior primeiro',
}

/** Próximo estado ao clicar na coluna: marca (padrão) → inverte → desmarca. */
export function alternarCriterio(
  criterios: CriterioOrdenacao[],
  coluna: ColunaOrdenacao,
): CriterioOrdenacao[] {
  const atual = criterios.find((c) => c.coluna === coluna)
  if (!atual) return [...criterios, { coluna, invertido: false }]
  if (!atual.invertido) return criterios.map((c) => (c.coluna === coluna ? { ...c, invertido: true } : c))
  return criterios.filter((c) => c.coluna !== coluna)
}

export interface ContextoOrdenacao {
  /** Nome da categoria da transação (ou null = "Sem categoria") */
  nomeCategoria: (t: TransacaoResponse) => string | null
  /** Posição da conta: a principal, as individuais (A-Z) e depois as conjuntas (A-Z) */
  posicaoConta: (t: TransacaoResponse) => number
}

// Texto em ordem alfabética (sem diferenciar maiúsculas e acentos); vazio vai para o fim
function compararTexto(a: string | null | undefined, b: string | null | undefined): number {
  const vazioA = !a?.trim()
  const vazioB = !b?.trim()
  if (vazioA || vazioB) return Number(vazioA) - Number(vazioB)
  return a!.localeCompare(b!, 'pt-BR', { sensitivity: 'base' })
}

// Sentido padrão de cada coluna
function comparar(coluna: ColunaOrdenacao, a: TransacaoResponse, b: TransacaoResponse, ctx: ContextoOrdenacao): number {
  switch (coluna) {
    case 'descricao':
      return compararTexto(a.descricao, b.descricao)
    case 'categoria':
      return compararTexto(ctx.nomeCategoria(a), ctx.nomeCategoria(b))
    case 'conta':
      return ctx.posicaoConta(a) - ctx.posicaoConta(b)
    case 'data':
      return b.dataTransacao.localeCompare(a.dataTransacao)
    case 'valor':
      return b.valor - a.valor
  }
}

/** Ordem padrão da tabela (e desempate final): data mais recente, depois a lançada por último. */
function compararPadrao(a: TransacaoResponse, b: TransacaoResponse): number {
  return b.dataTransacao.localeCompare(a.dataTransacao) || b.id - a.id
}

/** Nova lista ordenada pelos critérios marcados, na ordem em que foram marcados. */
export function ordenarTransacoes(
  transacoes: TransacaoResponse[],
  criterios: CriterioOrdenacao[],
  ctx: ContextoOrdenacao,
): TransacaoResponse[] {
  return [...transacoes].sort((a, b) => {
    for (const criterio of criterios) {
      const r = comparar(criterio.coluna, a, b, ctx)
      if (r !== 0) return criterio.invertido ? -r : r
    }
    return compararPadrao(a, b)
  })
}

/** Critérios no formato do parâmetro `ordenacao` da API: "conta,valor:inv" (vazio = padrão). */
export function paraParametroOrdenacao(criterios: CriterioOrdenacao[]): string | undefined {
  if (criterios.length === 0) return undefined
  return criterios.map((c) => (c.invertido ? `${c.coluna}:inv` : c.coluna)).join(',')
}
