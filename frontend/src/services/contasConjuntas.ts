/**
 * Serviço de API para Contas Conjuntas.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import type {
  ContaConjuntaResponse,
  CriarContaConjuntaRequest,
  EditarContaConjuntaRequest,
} from '../types/contaConjunta'

/** GET api/ContaConjunta — lista as contas conjuntas das quais o membro participa */
export async function listarContasConjuntas(): Promise<ContaConjuntaResponse[]> {
  const { data } = await api.get<ContaConjuntaResponse[]>('/ContaConjunta')
  return data
}

/** POST api/ContaConjunta — cria uma conta conjunta (o membro logado vira o criador) */
export async function criarContaConjunta(
  request: CriarContaConjuntaRequest,
): Promise<ContaConjuntaResponse> {
  const { data } = await api.post<ContaConjuntaResponse>('/ContaConjunta', request)
  return data
}

/** PUT api/ContaConjunta/{id} — edita nome/descrição (só o criador) */
export async function editarContaConjunta(
  id: number,
  request: EditarContaConjuntaRequest,
): Promise<ContaConjuntaResponse> {
  const { data } = await api.put<ContaConjuntaResponse>(`/ContaConjunta/${id}`, request)
  return data
}

/** DELETE api/ContaConjunta/{id} — exclui (soft delete) uma conta conjunta (só o criador) */
export async function excluirContaConjunta(id: number): Promise<void> {
  await api.delete(`/ContaConjunta/${id}`)
}

/** DELETE api/ContaConjunta/{id}/participantes/{participanteId} — remove um participante (só o criador) */
export async function removerParticipante(
  id: number,
  participanteId: number,
): Promise<ContaConjuntaResponse> {
  const { data } = await api.delete<ContaConjuntaResponse>(
    `/ContaConjunta/${id}/participantes/${participanteId}`,
  )
  return data
}

/** POST api/ContaConjunta/{id}/sair — o membro logado sai da conta conjunta (o criador não pode sair) */
export async function sairContaConjunta(id: number): Promise<void> {
  await api.post(`/ContaConjunta/${id}/sair`)
}
