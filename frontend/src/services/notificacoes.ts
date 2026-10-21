/**
 * Serviço de API para Notificações.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import type {
  NotificacaoResponse,
  EditarNotificacaoRequest,
  PreferenciasNotificacao,
} from '../types/notificacao'

/** GET api/Notificacao — notificações do membro logado (mais recentes primeiro) */
export async function listarNotificacoes(): Promise<NotificacaoResponse[]> {
  const { data } = await api.get<NotificacaoResponse[]>('/Notificacao')
  return data
}

/** PUT api/Notificacao/{id} — marca uma notificação como lida */
export async function marcarNotificacaoComoLida(id: number): Promise<NotificacaoResponse> {
  const request: EditarNotificacaoRequest = { lida: true }
  const { data } = await api.put<NotificacaoResponse>(`/Notificacao/${id}`, request)
  return data
}

/** GET api/Notificacao/preferencias — preferências do usuário (padrões se nunca salvou) */
export async function obterPreferenciasNotificacao(): Promise<PreferenciasNotificacao> {
  const { data } = await api.get<PreferenciasNotificacao>('/Notificacao/preferencias')
  return data
}

/** PUT api/Notificacao/preferencias — salva todas as preferências */
export async function salvarPreferenciasNotificacao(
  preferencias: PreferenciasNotificacao,
): Promise<PreferenciasNotificacao> {
  const { data } = await api.put<PreferenciasNotificacao>('/Notificacao/preferencias', preferencias)
  return data
}
