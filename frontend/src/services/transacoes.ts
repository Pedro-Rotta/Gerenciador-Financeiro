/**
 * Serviço de API para Transações.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import { nomeDoArquivo } from '../utils/download'
import type {
  ListarTransacaoRequest,
  TransacaoResponse,
  CriarTransacaoRequest,
  EditarTransacaoRequest,
} from '../types/transacao'
import { EscopoExclusao } from '../types/transacao'

/** GET api/Transacao?contaId={id} (ou ?contaConjuntaId={id}) — histórico de transações da conta */
export async function listarTransacoes(
  request: ListarTransacaoRequest,
): Promise<TransacaoResponse[]> {
  const { data } = await api.get<TransacaoResponse[]>('/Transacao', {
    params: { contaId: request.contaId, contaConjuntaId: request.contaConjuntaId },
  })
  return data
}

/** Filtros do PDF de transações — os mesmos da tabela "Transações recentes" (todos opcionais). */
export interface FiltrosPdfTransacoes {
  /** 0 = entradas, 1 = saídas (TipoTransacao); ausente = todas */
  tipo?: number
  /** "yyyy-MM-dd" */
  dataInicio?: string
  dataFim?: string
  contaId?: number
  contaConjuntaId?: number
  /** Nome da categoria */
  categoria?: string
  /** Rótulo do período escolhido (ex.: "Esta semana"), só para o cabeçalho do PDF */
  periodo?: string
  /** Mesma ordem da tabela: "conta,valor:inv" (colunas na ordem em que foram marcadas) */
  ordenacao?: string
}

/** GET api/Transacao/pdf — PDF das transações filtradas, gerado no backend (QuestPDF) */
export async function baixarPdfTransacoes(
  filtros: FiltrosPdfTransacoes,
): Promise<{ arquivo: Blob; nomeArquivo: string }> {
  const resposta = await api.get<Blob>('/Transacao/pdf', { params: filtros, responseType: 'blob' })
  return {
    arquivo: resposta.data,
    nomeArquivo: nomeDoArquivo(resposta.headers['content-disposition'], 'transacoes.pdf'),
  }
}

/** POST api/Transacao — cria uma nova transação (MembroId injetado) */
export async function criarTransacao(
  request: CriarTransacaoRequest,
): Promise<TransacaoResponse> {
  const { data } = await api.post<TransacaoResponse>('/Transacao', request)
  return data
}

/** PUT api/Transacao/{id} — edita descrição/status de uma transação (MembroId injetado) */
export async function editarTransacao(
  id: number,
  request: EditarTransacaoRequest,
): Promise<TransacaoResponse> {
  const { data } = await api.put<TransacaoResponse>(`/Transacao/${id}`, request)
  return data
}

/**
 * DELETE api/Transacao/{id} — exclui (soft delete) uma transação (MembroId injetado).
 * Em ocorrência de série, o escopo PROXIMAS exclui também as seguintes não pagas (pendentes ou atrasadas) e para a série.
 */
export async function excluirTransacao(
  id: number,
  escopo: EscopoExclusao = EscopoExclusao.ESTA,
): Promise<void> {
  await api.delete(`/Transacao/${id}`, { params: { escopo } })
}

/** POST api/Transacao/{id}/parar-recorrencia — a série da ocorrência para de repetir */
export async function pararRecorrencia(id: number): Promise<TransacaoResponse> {
  const { data } = await api.post<TransacaoResponse>(`/Transacao/${id}/parar-recorrencia`)
  return data
}
