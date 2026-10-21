/**
 * Tipos da entidade Meta, alinhados às DTOs do backend (.NET).
 */

/** GET api/Meta — meta de economia do membro logado */
export interface MetaResponse {
  id: number
  nome: string
  valorAlvo: number
  valorAtual: number
  prazo: string
  concluida: boolean
  membroId: number | null
  ativo: boolean
  transacoesVinculadas: { id: number; metaId: number; transacaoId: number; vinculadoEm: string }[]
}

/** POST api/Meta — cria uma nova meta (MembroId injetado) */
export interface CriarMetaRequest {
  nome: string
  valorAlvo: number
  prazo: string
}

/** PUT api/Meta/{id} — edita nome/valor alvo/prazo de uma meta */
export interface EditarMetaRequest {
  nome?: string | null
  valorAlvo?: number | null
  prazo?: string | null
}
