/**
 * Contas selecionáveis (individuais + conjuntas) para os seletores de conta
 * das telas de Transações e Categorias.
 */

import { listarContas } from './contas'
import { listarContasConjuntas } from './contasConjuntas'
import type { ContaResponse } from '../types/conta'
import type { ContaConjuntaResponse } from '../types/contaConjunta'

/** Conta escolhível em um seletor: individual ou conjunta. */
export interface OpcaoConta {
  /** Valor do seletor: "c:ID" (conta individual) ou "cc:ID" (conta conjunta) */
  chave: string
  id: number
  nome: string
  conjunta: boolean
  principal: boolean
  /** Aceita transações: conta conjunta sempre; individual só depois do saldo inicial */
  liberada: boolean
}

/** Ordem dos seletores: a conta principal sempre em 1º; as demais por nome (A-Z). */
export function ordenarOpcoesConta(a: OpcaoConta, b: OpcaoConta): number {
  if (a.principal !== b.principal) return a.principal ? -1 : 1
  return a.nome.localeCompare(b.nome, 'pt-BR', { sensitivity: 'base' })
}

/** Busca as contas individuais e conjuntas do membro logado, já ordenadas. */
export async function carregarOpcoesConta(): Promise<OpcaoConta[]> {
  const [contas, conjuntas] = await Promise.all([listarContas(), listarContasConjuntas()])
  return montarOpcoesConta(contas, conjuntas)
}

/** Monta as opções (já ordenadas) a partir de contas e contas conjuntas já carregadas. */
export function montarOpcoesConta(contas: ContaResponse[], conjuntas: ContaConjuntaResponse[]): OpcaoConta[] {
  const individuais: OpcaoConta[] = contas.map((c) => ({
    chave: `c:${c.id}`, id: c.id, nome: c.nome, conjunta: false, principal: c.contaPrincipal, liberada: c.saldoInicialDefinido,
  }))
  const compartilhadas: OpcaoConta[] = conjuntas.map((c) => ({
    chave: `cc:${c.id}`, id: c.id, nome: c.nome, conjunta: true, principal: false, liberada: true,
  }))
  return [...individuais.sort(ordenarOpcoesConta), ...compartilhadas.sort(ordenarOpcoesConta)]
}

/** Filtro de API para a conta escolhida: { contaId } ou { contaConjuntaId } */
export function filtroDaConta(opcao: OpcaoConta): { contaId: number } | { contaConjuntaId: number } {
  return opcao.conjunta ? { contaConjuntaId: opcao.id } : { contaId: opcao.id }
}

/** Indica se um registro (categoria/subcategoria) pertence à conta escolhida. */
export function pertenceAConta(
  registro: { contaId: number | null; contaConjuntaId: number | null },
  opcao: OpcaoConta,
): boolean {
  return opcao.conjunta ? registro.contaConjuntaId === opcao.id : registro.contaId === opcao.id
}
