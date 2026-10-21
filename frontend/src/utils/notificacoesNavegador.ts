/**
 * Notificações do navegador (Configurações → Notificações): enquanto o app está aberto, as
 * notificações novas do sino também aparecem como aviso do sistema quando o membro está em
 * outra aba ou outro programa. A escolha vale para este navegador (localStorage) e depende
 * da permissão do navegador. Respeita o horário de silêncio (22h às 7h) das preferências.
 */

import { obterPreferenciasNotificacao } from '../services/notificacoes'
import type { NotificacaoResponse } from '../types/notificacao'

const CHAVE = 'notificacoesNavegador'

export function navegadorSuportaNotificacoes(): boolean {
  return typeof window !== 'undefined' && 'Notification' in window
}

/** Ligadas neste navegador e com a permissão concedida. */
export function notificacoesNavegadorLigadas(): boolean {
  if (!navegadorSuportaNotificacoes() || Notification.permission !== 'granted') return false
  try {
    return localStorage.getItem(CHAVE) === 'sim'
  } catch {
    return false
  }
}

/** Pede a permissão (se preciso) e liga; devolve o motivo quando não dá. */
export async function ligarNotificacoesNavegador(): Promise<'ligado' | 'negado' | 'sem-suporte'> {
  if (!navegadorSuportaNotificacoes()) return 'sem-suporte'
  const permissao =
    Notification.permission === 'default' ? await Notification.requestPermission() : Notification.permission
  if (permissao !== 'granted') return 'negado'
  try {
    localStorage.setItem(CHAVE, 'sim')
  } catch {
    // sem localStorage a escolha vale só até recarregar
  }
  return 'ligado'
}

export function desligarNotificacoesNavegador(): void {
  try {
    localStorage.removeItem(CHAVE)
  } catch {
    // nada a desligar
  }
}

function emHorarioDeSilencio(agora: Date): boolean {
  const hora = agora.getHours()
  return hora >= 22 || hora < 7
}

/** Mostra as notificações novas como aviso do navegador, se ligadas e o app não estiver à vista. */
export async function mostrarNoNavegador(novas: NotificacaoResponse[]): Promise<void> {
  if (novas.length === 0 || !notificacoesNavegadorLigadas()) return
  // Com o app na frente, o sino já basta
  if (document.visibilityState === 'visible' && document.hasFocus()) return
  try {
    const preferencias = await obterPreferenciasNotificacao()
    if (preferencias.horarioSilencio && emHorarioDeSilencio(new Date())) return
  } catch {
    // sem as preferências, mostra mesmo assim
  }
  for (const n of novas) {
    const aviso = new Notification('Gerenciador Familiar', { body: n.mensagem, tag: `notificacao-${n.id}` })
    aviso.onclick = () => {
      window.focus()
      aviso.close()
    }
  }
}
