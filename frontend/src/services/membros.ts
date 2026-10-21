/**
 * Serviço de API para Membros.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import type {
  CriarMembroRequest,
  EditarPerfilRequest,
  PerfilResponse,
  PreferenciasUso,
} from '../types/membro'

/** GET api/Membro/preferencias — preferências de uso (padrões se o membro nunca salvou) */
export async function obterPreferenciasUso(): Promise<PreferenciasUso> {
  const { data } = await api.get<PreferenciasUso>('/Membro/preferencias')
  return data
}

/** PUT api/Membro/preferencias — grava todas as preferências de uso */
export async function salvarPreferenciasUso(request: PreferenciasUso): Promise<PreferenciasUso> {
  const { data } = await api.put<PreferenciasUso>('/Membro/preferencias', request)
  return data
}

/** DELETE api/Membro — exclui a própria conta (confirmada com a senha); a sessão cai */
export async function excluirConta(senha: string): Promise<void> {
  await api.delete('/Membro', { data: { senha } })
}

/** GET api/Membro — perfil do membro logado */
export async function obterPerfil(): Promise<PerfilResponse> {
  const { data } = await api.get<PerfilResponse>('/Membro')
  return data
}

/** PUT api/Membro — edita o próprio perfil (o membro vem da sessão) */
export async function editarPerfil(request: EditarPerfilRequest): Promise<PerfilResponse> {
  const { data } = await api.put<PerfilResponse>('/Membro', request)
  return data
}

/** POST api/Membro/encerrar-sessoes — derruba as outras sessões; este navegador recebe a nova */
export async function encerrarOutrasSessoes(): Promise<{ expiraEm: string }> {
  const { data } = await api.post<{ expiraEm: string }>('/Membro/encerrar-sessoes')
  return data
}

/** POST api/Membro — cria um novo membro (cadastro) */
export async function criarMembro(request: CriarMembroRequest): Promise<void> {
  await api.post('/Membro', request)
}

/**
 * PUT api/Membro/senha — troca a senha (com a atual); este navegador recebe um cookie novo, os
 * outros caem. Devolve a validade da sessão nova.
 */
export async function alterarSenha(request: {
  senhaAtual: string
  novaSenha: string
  confirmacaoSenha: string
}): Promise<{ expiraEm: string }> {
  const { data } = await api.put<{ expiraEm: string }>('/Membro/senha', request)
  return data
}
