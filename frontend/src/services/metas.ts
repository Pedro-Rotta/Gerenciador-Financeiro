/**
 * Serviço de API para Metas.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import type { MetaResponse, CriarMetaRequest, EditarMetaRequest } from '../types/meta'

/** GET api/Meta — lista as metas do membro logado (MembroId injetado) */
export async function listarMetas(): Promise<MetaResponse[]> {
  const { data } = await api.get<MetaResponse[]>('/Meta')
  return data
}

/** POST api/Meta — cria uma nova meta (MembroId injetado) */
export async function criarMeta(
  request: CriarMetaRequest,
): Promise<MetaResponse> {
  const { data } = await api.post<MetaResponse>('/Meta', request)
  return data
}

/** PUT api/Meta/{id} — edita nome/valor alvo/prazo de uma meta (MembroId injetado) */
export async function editarMeta(
  id: number,
  request: EditarMetaRequest,
): Promise<MetaResponse> {
  const { data } = await api.put<MetaResponse>(`/Meta/${id}`, request)
  return data
}

/** DELETE api/Meta/{id} — exclui (soft delete) uma meta (MembroId injetado) */
export async function excluirMeta(id: number): Promise<void> {
  await api.delete(`/Meta/${id}`)
}
