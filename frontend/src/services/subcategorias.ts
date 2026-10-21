/**
 * Serviço de API para SubCategorias.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import { listarContas } from './contas'
import type {
  CriarSubCategoriaRequest,
  EditarSubCategoriaRequest,
  VincularSubCategoriaRequest,
  DesvincularSubCategoriaRequest,
  SubCategoriaResponse,
} from '../types/subcategoria'
import type { CategoriaResponse } from '../types/categoria'

/** POST api/SubCategoria — cria uma nova subcategoria */
export async function criarSubCategoria(
  request: CriarSubCategoriaRequest,
): Promise<SubCategoriaResponse> {
  const { data } = await api.post<SubCategoriaResponse>('/SubCategoria', request)
  return data
}

/**
 * GET api/SubCategoria?contaId=X — lista as subcategorias de uma conta.
 * Usado para escolher qual subcategoria vincular a uma categoria.
 */
export async function listarSubCategoriasPorConta(
  contaId: number,
): Promise<SubCategoriaResponse[]> {
  const { data } = await api.get<SubCategoriaResponse[]>('/SubCategoria', {
    params: { contaId },
  })
  return data
}

/**
 * GET api/SubCategoria?contaConjuntaId=X — lista as subcategorias de uma conta conjunta.
 */
export async function listarSubCategoriasPorContaConjunta(
  contaConjuntaId: number,
): Promise<SubCategoriaResponse[]> {
  const { data } = await api.get<SubCategoriaResponse[]>('/SubCategoria', {
    params: { contaConjuntaId },
  })
  return data
}

/** POST api/SubCategoria/vincular — vincula uma subcategoria existente à categoria */
export async function vincularSubCategoria(
  request: VincularSubCategoriaRequest,
): Promise<CategoriaResponse> {
  const { data } = await api.post<CategoriaResponse>('/SubCategoria/vincular', request)
  return data
}

/** POST api/SubCategoria/desvincular — desvincula uma subcategoria de uma categoria */
export async function desvincularSubCategoria(
  request: DesvincularSubCategoriaRequest,
): Promise<CategoriaResponse> {
  const { data } = await api.post<CategoriaResponse>('/SubCategoria/desvincular', request)
  return data
}

/**
 * Lista todas as subcategorias do membro logado, agregando as subcategorias
 * de todas as suas contas individuais (o backend exige contaId por consulta).
 */
export async function listarTodasSubCategorias(): Promise<SubCategoriaResponse[]> {
  const contas = await listarContas()
  const listas = await Promise.all(
    contas.map((conta) => listarSubCategoriasPorConta(conta.id)),
  )
  return listas.flat()
}

/** PUT api/SubCategoria/{id} — edita o nome de uma subcategoria */
export async function editarSubCategoria(
  id: number,
  request: EditarSubCategoriaRequest,
): Promise<SubCategoriaResponse> {
  const { data } = await api.put<SubCategoriaResponse>(`/SubCategoria/${id}`, request)
  return data
}

/** DELETE api/SubCategoria/{id} — exclui (soft delete) uma subcategoria */
export async function excluirSubCategoria(id: number): Promise<void> {
  await api.delete(`/SubCategoria/${id}`)
}
