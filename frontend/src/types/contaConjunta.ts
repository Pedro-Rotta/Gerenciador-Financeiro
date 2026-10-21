/**
 * Tipos da entidade ContaConjunta, alinhados às DTOs do backend (.NET).
 */

/** Participante de uma conta conjunta (MembroResumoResponse) */
export interface MembroResumo {
  id: number
  nome: string
  nomeDeUsuario: string
}

/** GET api/ContaConjunta */
export interface ContaConjuntaResponse {
  id: number
  nome: string
  /** Saldo real: só o que foi pago/recebido */
  saldo: number
  /** Saldo + pendentes/atrasadas (vem só na listagem) */
  saldoPrevisto?: number | null
  descricao: string | null
  criadorId: number
  ativo: boolean
  membros: MembroResumo[]
}

/** POST api/ContaConjunta (o criador vem do JWT) */
export interface CriarContaConjuntaRequest {
  nome: string
  descricao?: string | null
}

/** PUT api/ContaConjunta/{id} — descrição "" limpa o campo */
export interface EditarContaConjuntaRequest {
  nome?: string | null
  descricao?: string | null
}
