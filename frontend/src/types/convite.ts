/**
 * Tipos de Convite para conta conjunta, alinhados às DTOs do backend (.NET).
 */

/**
 * Enum ConviteContaConjunta.StatusConvite do backend:
 *   PENDENTE = 0, ACEITO = 1, RECUSADO = 2, CANCELADO = 3
 */
export const StatusConvite = {
  PENDENTE: 0,
  ACEITO: 1,
  RECUSADO: 2,
  CANCELADO: 3,
} as const

export type StatusConvite = 0 | 1 | 2 | 3

/** Response de Convite */
export interface ConviteResponse {
  id: number
  contaConjuntaId: number
  contaConjuntaNome: string
  convidadoId: number
  convidadoNome: string
  convidadoNomeDeUsuario: string
  convidadoPorId: number
  convidadoPorNome: string
  status: StatusConvite
  criadoEm: string
  respondidoEm: string | null
}

/** POST api/Convite — só o criador da conta conjunta (MembroId injetado) */
export interface CriarConviteRequest {
  contaConjuntaId: number
  /** Nome de usuário ou e-mail do convidado */
  usuarioOuEmail: string
}
