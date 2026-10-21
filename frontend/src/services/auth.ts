/**
 * Serviço de API para Autenticação.
 * A sessão fica num cookie HttpOnly gravado pela API: o front nunca vê o token.
 */

import api from './api'
import type { LoginResponse } from '../types/auth'

/** POST api/Auth/login — autentica; a API grava o cookie de sessão */
export async function login(
  nomeDeUsuario: string,
  senha: string,
): Promise<LoginResponse> {
  const { data } = await api.post<LoginResponse>('/Auth/login', {
    nomeDeUsuario,
    senha,
  })
  return data
}

/** POST api/Auth/esqueci-senha — manda o código para o e-mail da conta; devolve a mensagem (sempre a mesma) */
export async function pedirCodigoSenha(identificador: string): Promise<string> {
  const { data } = await api.post<{ mensagem: string }>('/Auth/esqueci-senha', { identificador })
  return data.mensagem
}

/** POST api/Auth/redefinir-senha — troca a senha com o código recebido; as sessões abertas caem */
export async function redefinirSenha(request: {
  identificador: string
  codigo: string
  novaSenha: string
  confirmacaoSenha: string
}): Promise<string> {
  const { data } = await api.post<{ mensagem: string }>('/Auth/redefinir-senha', request)
  return data.mensagem
}

/** POST api/Auth/logout — apaga o cookie de sessão */
export async function logout(): Promise<void> {
  await api.post('/Auth/logout')
}
