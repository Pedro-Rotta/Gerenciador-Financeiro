/**
 * Filtros de transações (tipo, período, conta e categoria) usados pela barra
 * FiltrosTransacoes no Dashboard e na página de Categorias.
 */

import { dataLocalISO } from './data'
import { preferenciasUso } from './preferenciasUso'
import type { TransacaoResponse } from '../types/transacao'
import { TipoTransacao } from '../types/transacao'

export type FiltroTipo = 'todas' | 'entradas' | 'saidas'
export type FiltroPeriodo =
  | 'semana'
  | 'mes'
  | 'mes-anterior'
  | '3-meses'
  | '6-meses'
  | 'ano'
  | 'tudo'
  | 'personalizado'

export interface Filtros {
  tipo: FiltroTipo
  periodo: FiltroPeriodo
  /** Início e fim ("yyyy-MM-dd") do período personalizado */
  de: string
  ate: string
  /** Chave da conta ("c:ID" ou "cc:ID"); vazio = todas as contas */
  conta: string
  /** Nome da categoria; vazio = todas (o nome agrupa a mesma categoria em contas diferentes) */
  categoria: string
}

/** Categoria disponível no seletor (nome único por tipo). */
export interface OpcaoCategoria {
  nome: string
  tipo: 'entrada' | 'saida'
}

export const PERIODOS: { valor: FiltroPeriodo; rotulo: string; descricao: string }[] = [
  { valor: 'semana', rotulo: 'Esta semana', descricao: 'desta semana' },
  { valor: 'mes', rotulo: 'Este mês', descricao: 'deste mês' },
  { valor: 'mes-anterior', rotulo: 'Mês passado', descricao: 'do mês passado' },
  { valor: '3-meses', rotulo: 'Últimos 3 meses', descricao: 'dos últimos 3 meses' },
  { valor: '6-meses', rotulo: 'Últimos 6 meses', descricao: 'dos últimos 6 meses' },
  { valor: 'ano', rotulo: 'Este ano', descricao: 'deste ano' },
  { valor: 'tudo', rotulo: 'Todo o período', descricao: 'de todo o período' },
  { valor: 'personalizado', rotulo: 'Personalizado', descricao: 'do período escolhido' },
]

/** Texto do período para subtítulos (ex.: "desta semana"). */
export function descricaoPeriodo(periodo: FiltroPeriodo): string {
  return PERIODOS.find((p) => p.valor === periodo)?.descricao ?? ''
}

/** Período (com as datas do personalizado), sem os demais filtros. */
export type Periodo = Pick<Filtros, 'periodo' | 'de' | 'ate'>

/** Intervalo de datas (inclusivo) do período escolhido; null = sem limite. */
export function intervaloDoPeriodo(filtros: Periodo): { de: string | null; ate: string | null } {
  const hoje = new Date()
  const ano = hoje.getFullYear()
  const mes = hoje.getMonth()
  switch (filtros.periodo) {
    case 'semana': {
      // Semana começando na segunda-feira ou no domingo (Configurações → Preferências)
      const diasDesdeOInicio = preferenciasUso().semanaComecaNoDomingo
        ? hoje.getDay()
        : (hoje.getDay() + 6) % 7
      const inicio = new Date(hoje)
      inicio.setDate(hoje.getDate() - diasDesdeOInicio)
      return { de: dataLocalISO(inicio), ate: null }
    }
    case 'mes':
      return { de: dataLocalISO(new Date(ano, mes, 1)), ate: null }
    case 'mes-anterior':
      return { de: dataLocalISO(new Date(ano, mes - 1, 1)), ate: dataLocalISO(new Date(ano, mes, 0)) }
    case '3-meses':
      return { de: dataLocalISO(new Date(ano, mes - 2, 1)), ate: null }
    case '6-meses':
      return { de: dataLocalISO(new Date(ano, mes - 5, 1)), ate: null }
    case 'ano':
      return { de: dataLocalISO(new Date(ano, 0, 1)), ate: null }
    case 'personalizado':
      return { de: filtros.de || null, ate: filtros.ate || null }
    default:
      return { de: null, ate: null }
  }
}

/** Chave da conta de uma transação, no mesmo formato de OpcaoConta.chave. */
export function chaveDaConta(t: TransacaoResponse): string {
  return t.contaId != null ? `c:${t.contaId}` : `cc:${t.contaConjuntaId}`
}

/**
 * Aplica os filtros a uma lista de transações.
 * `nomeCategoria` resolve o nome da categoria de cada transação (ou null).
 */
export function filtrarTransacoes(
  transacoes: TransacaoResponse[],
  filtros: Filtros,
  nomeCategoria: (t: TransacaoResponse) => string | null,
): TransacaoResponse[] {
  const { de, ate } = intervaloDoPeriodo(filtros)
  return transacoes.filter((t) => {
    if (filtros.tipo === 'entradas' && t.tipo !== TipoTransacao.ENTRADA) return false
    if (filtros.tipo === 'saidas' && t.tipo !== TipoTransacao.SAIDA) return false
    if (de && t.dataTransacao < de) return false
    if (ate && t.dataTransacao > ate) return false
    if (filtros.conta && chaveDaConta(t) !== filtros.conta) return false
    if (filtros.categoria && nomeCategoria(t) !== filtros.categoria) return false
    return true
  })
}
