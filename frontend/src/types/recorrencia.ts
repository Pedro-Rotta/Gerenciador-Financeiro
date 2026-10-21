/**
 * Tipos das séries recorrentes, alinhados às DTOs do backend (.NET).
 */

import type { TipoTransacao } from './transacao'

/** GET api/Recorrencia/previstas — ocorrência mensal futura, ainda não lançada (fora do saldo) */
export interface OcorrenciaPrevistaResponse {
  recorrenciaId: number
  /** "yyyy-MM-dd" */
  dataPrevista: string
  /** Posição na série (ex.: 6 em "6 de 10") */
  numeroOcorrencia: number
  /** Quantidade de vezes da série (null = sem fim) */
  totalOcorrencias: number | null
  valor: number
  descricao: string | null
  tipo: TipoTransacao
  membroId: number
  categoriaId: number | null
  contaId: number | null
  contaConjuntaId: number | null
}
