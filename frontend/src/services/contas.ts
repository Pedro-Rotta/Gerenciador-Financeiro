/**
 * Serviço de API para Contas.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import type {
  ContaResponse,
  CriarContaRequest,
  DefinirSaldoInicialRequest,
  EditarContaRequest,
} from '../types/conta'

/** GET api/Conta — lista as contas do membro logado */
export async function listarContas(): Promise<ContaResponse[]> {
  const { data } = await api.get<ContaResponse[]>('/Conta')
  return data
}

/** POST api/Conta — cria uma nova conta (nome + contaPrincipal) */
export async function criarConta(
  request: CriarContaRequest,
): Promise<ContaResponse> {
  const { data } = await api.post<ContaResponse>('/Conta', request)
  return data
}

/** PUT api/Conta/{id} — edita o nome de uma conta */
export async function editarConta(
  id: number,
  request: EditarContaRequest,
): Promise<ContaResponse> {
  const { data } = await api.put<ContaResponse>(`/Conta/${id}`, request)
  return data
}

/** DELETE api/Conta/{id} — exclui (soft delete) uma conta */
export async function excluirConta(id: number): Promise<void> {
  await api.delete(`/Conta/${id}`)
}

/** PUT api/Conta/{id}/principal — define esta conta como a principal (MembroId injetado) */
export async function alterarContaPrincipal(id: number): Promise<ContaResponse> {
  const { data } = await api.put<ContaResponse>(`/Conta/${id}/principal`)
  return data
}

/** POST api/Conta/{id}/saldo-inicial — informa o saldo inicial (uma vez só) e libera os lançamentos */
export async function definirSaldoInicial(
  id: number,
  request: DefinirSaldoInicialRequest,
): Promise<ContaResponse> {
  const { data } = await api.post<ContaResponse>(`/Conta/${id}/saldo-inicial`, request)
  return data
}
