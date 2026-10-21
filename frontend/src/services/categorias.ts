/**
 * Serviço de API para Categorias.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import { listarContas } from './contas'
import type {
  CriarCategoriaRequest,
  EditarCategoriaRequest,
  CategoriaResponse,
} from '../types/categoria'

/** POST api/Categoria — cria uma nova categoria */
export async function criarCategoria(
  request: CriarCategoriaRequest,
): Promise<CategoriaResponse> {
  const { data } = await api.post<CategoriaResponse>('/Categoria', request)
  return data
}

/**
 * GET api/Categoria?contaId=X — lista as categorias de uma conta específica.
 */
export async function listarCategoriasPorConta(
  contaId: number,
): Promise<CategoriaResponse[]> {
  const { data } = await api.get<CategoriaResponse[]>('/Categoria', {
    params: { contaId },
  })
  return data
}

/**
 * GET api/Categoria?contaConjuntaId=X — lista as categorias de uma conta conjunta.
 */
export async function listarCategoriasPorContaConjunta(
  contaConjuntaId: number,
): Promise<CategoriaResponse[]> {
  const { data } = await api.get<CategoriaResponse[]>('/Categoria', {
    params: { contaConjuntaId },
  })
  return data
}

/**
 * Lista todas as categorias do membro logado, agregando as categorias
 * de todas as suas contas individuais. (O backend não expõe um endpoint
 * "todas as categorias do membro", então consultamos conta a conta.)
 */
export async function listarTodasCategorias(): Promise<CategoriaResponse[]> {
  const contas = await listarContas()
  const listas = await Promise.all(
    contas.map((conta) => listarCategoriasPorConta(conta.id)),
  )
  return listas.flat()
}

/** PUT api/Categoria/{id} — edita uma categoria existente */
export async function editarCategoria(
  id: number,
  request: EditarCategoriaRequest,
): Promise<CategoriaResponse> {
  const { data } = await api.put<CategoriaResponse>(`/Categoria/${id}`, request)
  return data
}

/** DELETE api/Categoria/{id} — exclui (soft delete) uma categoria */
export async function excluirCategoria(id: number): Promise<void> {
  await api.delete(`/Categoria/${id}`)
}
