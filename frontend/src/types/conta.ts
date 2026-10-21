/**
 * Tipos da entidade Conta, alinhados às DTOs do backend (.NET).
 */

/** GET api/Conta */
export interface ContaResponse {
  id: number
  nome: string
  /** Saldo real: só o que foi pago/recebido */
  saldo: number
  /** Saldo + pendentes/atrasadas (vem só na listagem) */
  saldoPrevisto?: number | null
  /** Falso enquanto a conta não recebeu o saldo inicial (não aceita transações) */
  saldoInicialDefinido: boolean
  membroId: number
  ativo: boolean
  contaPrincipal: boolean
}

/** POST api/Conta/{id}/saldo-inicial */
export interface DefinirSaldoInicialRequest {
  /** Pode ser negativo ou zero */
  valor: number
}

/** POST api/Conta */
export interface CriarContaRequest {
  nome: string
  contaPrincipal: boolean
}

/** PUT api/Conta/{id} */
export interface EditarContaRequest {
  nome?: string | null
}
