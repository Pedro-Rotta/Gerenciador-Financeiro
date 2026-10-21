/**
 * Tipos da entidade SubCategoria, alinhados às DTOs do backend (.NET).
 */

/** Response de SubCategoria */
export interface SubCategoriaResponse {
  id: number
  nome: string
  ativo: boolean
  contaId: number | null
  contaConjuntaId: number | null
}

/** POST api/SubCategoria */
export interface CriarSubCategoriaRequest {
  nome: string
  contaId?: number | null
  contaConjuntaId?: number | null
}

/** PUT api/SubCategoria/{id} */
export interface EditarSubCategoriaRequest {
  nome?: string | null
}

/** POST api/SubCategoria/vincular */
export interface VincularSubCategoriaRequest {
  idCategoria: number
  idSubCategoria: number
}

/** POST api/SubCategoria/desvincular */
export interface DesvincularSubCategoriaRequest {
  idCategoria: number
  idSubCategoria: number
}
