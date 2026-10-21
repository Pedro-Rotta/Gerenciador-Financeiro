/**
 * Tipos da entidade Notificacao, alinhados às DTOs do backend (.NET).
 */

/**
 * Enums de Notificacao do backend:
 *   TipoNotificacao:      GERAL=0, CONTA_CONJUNTA=1, TRANSACAO=2, SALDO=3, META=4
 *   GravidadeNotificacao: INFORMATIVA=0, ATENCAO=1, CRITICA=2
 * O tipo define o ícone e a gravidade a cor do ícone (verde, amarelo, vermelho).
 */
export const TipoNotificacao = {
  GERAL: 0,
  CONTA_CONJUNTA: 1,
  TRANSACAO: 2,
  SALDO: 3,
  META: 4,
} as const

export type TipoNotificacao = 0 | 1 | 2 | 3 | 4

export const GravidadeNotificacao = {
  INFORMATIVA: 0,
  ATENCAO: 1,
  CRITICA: 2,
} as const

export type GravidadeNotificacao = 0 | 1 | 2

/** GET api/Notificacao */
export interface NotificacaoResponse {
  id: number
  mensagem: string
  lida: boolean
  tipo: TipoNotificacao
  gravidade: GravidadeNotificacao
  membroId: number
  criadoEm: string
}

/** PUT api/Notificacao/{id} */
export interface EditarNotificacaoRequest {
  mensagem?: string | null
  lida?: boolean | null
}

/** GET/PUT api/Notificacao/preferencias — o PUT recebe o corpo completo */
export interface PreferenciasNotificacao {
  lancamentosOutros: boolean
  vencimento: boolean
  /** 1, 3 ou 7 */
  diasAntesVencimento: number
  atrasada: boolean
  saldoBaixo: boolean
  /** Obrigatório (> 0) com saldoBaixo ligado */
  valorSaldoBaixo: number | null
  gastoAcimaNormal: boolean
  metaProgresso: boolean
  metaPrazo: boolean
  horarioSilencio: boolean
}
