/**
 * Tipos e enums da entidade Transacao, alinhados às DTOs do backend (.NET).
 */

/**
 * Enums de Transacao do backend:
 *   TipoTransacao:        ENTRADA=0, SAIDA=1
 *   ClassificacaoTransacao: FIXA=0, VARIAVEL=1
 *   StatusTransacao:      PENDENTE=0, CONCLUIDA=1, ATRASADA=2
 *   PeriodicidadeTransacao: MENSAL=0, SEMANAL=1
 * O ASP.NET (System.Text.Json) espera o enum como número no corpo da requisição.
 */
export const TipoTransacao = {
  ENTRADA: 0,
  SAIDA: 1,
} as const

export type TipoTransacao = 0 | 1

export const StatusTransacao = {
  PENDENTE: 0,
  CONCLUIDA: 1,
  ATRASADA: 2,
} as const

export type StatusTransacao = 0 | 1 | 2

export const ClassificacaoTransacao = {
  FIXA: 0,
  VARIAVEL: 1,
} as const

export type ClassificacaoTransacao = 0 | 1

export const PeriodicidadeTransacao = {
  MENSAL: 0,
  SEMANAL: 1,
} as const

export type PeriodicidadeTransacao = 0 | 1

/** GET api/Transacao?contaId={id} ou ?contaConjuntaId={id} — informe exatamente um */
export interface ListarTransacaoRequest {
  contaId?: number
  contaConjuntaId?: number
}

/** Response de Transacao */
export interface TransacaoResponse {
  id: number
  valor: number
  descricao: string | null
  dataTransacao: string
  tipo: TipoTransacao
  classificacao: number
  recorrente: boolean
  periodicidade: number | null
  status: StatusTransacao
  /** Autor da transação */
  membroId: number
  categoriaId: number | null
  subCategoriaId: number | null
  contaId: number | null
  contaConjuntaId: number | null
  /** Série recorrente que gerou a transação (null = lançamento avulso) */
  recorrenciaId: number | null
  /** Posição na série (ex.: 3 em "3 de 10") */
  numeroOcorrencia: number | null
  /** Quantidade de vezes da série (null = sem fim) */
  totalOcorrencias: number | null
  /** A série ainda repete (false = encerrada) */
  serieAtiva: boolean | null
  ativo: boolean
}

/** POST api/Transacao — cria uma nova transação */
export interface CriarTransacaoRequest {
  valor: number
  descricao?: string | null
  dataTransacao: string // DateOnly "yyyy-MM-dd"
  tipo: TipoTransacao
  classificacao: ClassificacaoTransacao
  recorrente: boolean
  periodicidade?: PeriodicidadeTransacao | null
  /** Só para recorrente: quantidade de vezes (2 a 120); vazio = sem fim */
  quantidadeOcorrencias?: number | null
  status: StatusTransacao
  categoriaId?: number | null
  subCategoriaId?: number | null
  /** Informe exatamente um: contaId (individual) OU contaConjuntaId */
  contaId?: number | null
  contaConjuntaId?: number | null
}

/** DELETE api/Transacao/{id}?escopo= — em ocorrência de série: só esta ou esta e as próximas */
export const EscopoExclusao = {
  ESTA: 0,
  PROXIMAS: 1,
} as const

export type EscopoExclusao = 0 | 1

/** PUT api/Transacao/{id} — edita descrição e/ou status de uma transação */
export interface EditarTransacaoRequest {
  descricao?: string | null
  status?: StatusTransacao
}
