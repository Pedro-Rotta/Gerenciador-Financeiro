/**
 * Serviço de API para Convites de contas conjuntas.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import type { ConviteResponse, CriarConviteRequest } from '../types/convite'

/** Evento disparado quando o membro entra numa conta conjunta (convite aceito). */
export const EVENTO_CONTAS_CONJUNTAS_ALTERADAS = 'contas-conjuntas-alteradas'

/** POST api/Convite — convida um membro (por usuário ou e-mail) para a conta conjunta */
export async function criarConvite(request: CriarConviteRequest): Promise<ConviteResponse> {
  const { data } = await api.post<ConviteResponse>('/Convite', request)
  return data
}

/** GET api/Convite/recebidos — convites pendentes recebidos pelo membro logado */
export async function listarConvitesRecebidos(): Promise<ConviteResponse[]> {
  const { data } = await api.get<ConviteResponse[]>('/Convite/recebidos')
  return data
}

/** GET api/Convite?contaConjuntaId=X — convites pendentes de uma conta conjunta (só o criador) */
export async function listarConvitesDaConta(contaConjuntaId: number): Promise<ConviteResponse[]> {
  const { data } = await api.get<ConviteResponse[]>('/Convite', {
    params: { contaConjuntaId },
  })
  return data
}

/** POST api/Convite/{id}/aceitar — o convidado entra na conta conjunta */
export async function aceitarConvite(id: number): Promise<ConviteResponse> {
  const { data } = await api.post<ConviteResponse>(`/Convite/${id}/aceitar`)
  return data
}

/** POST api/Convite/{id}/recusar — o convidado recusa o convite */
export async function recusarConvite(id: number): Promise<ConviteResponse> {
  const { data } = await api.post<ConviteResponse>(`/Convite/${id}/recusar`)
  return data
}

/** DELETE api/Convite/{id} — o criador cancela um convite pendente */
export async function cancelarConvite(id: number): Promise<void> {
  await api.delete(`/Convite/${id}`)
}
