/**
 * Tipos da entidade Categoria, alinhados às DTOs do backend (.NET).
 */

import type { SubCategoriaResponse } from './subcategoria'

/**
 * Enum Categoria.TipoCategoria do backend:
 *   ENTRADA = 0
 *   SAIDA   = 1
 * O ASP.NET (System.Text.Json) espera o enum como número no corpo da requisição.
 */
export type TipoCategoria = 0 | 1

export const TipoCategoria = {
  ENTRADA: 0,
  SAIDA: 1,
} as const

/** Response de Categoria */
export interface CategoriaResponse {
  id: number
  nome: string
  tipo: TipoCategoria
  necessario: boolean
  icone: string | null
  ativo: boolean
  contaId: number | null
  contaConjuntaId: number | null
  subCategorias: SubCategoriaResponse[]
}

/** POST api/Categoria */
export interface CriarCategoriaRequest {
  nome: string
  tipo: TipoCategoria
  necessario: boolean
  icone?: string | null
  contaId?: number | null
  contaConjuntaId?: number | null
}

/** PUT api/Categoria/{id} */
export interface EditarCategoriaRequest {
  nome?: string | null
  tipo?: TipoCategoria | null
  necessario?: boolean
  icone?: string | null
}
