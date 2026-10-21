/**
 * Serviço de API da verificação ao abrir o app.
 * O servidor roda os passos (gerar ocorrências das séries, marcar atrasadas, lembretes) no
 * máximo uma vez por hora — ou de novo quando muda o dia — para cada usuário.
 */

import api from './api'

// Chamadas simultâneas (sino e painel ao abrir o app) esperam a mesma verificação
let emAndamento: Promise<void> | null = null

/** POST api/Verificacao — dispara a verificação do usuário logado */
export function executarVerificacao(): Promise<void> {
  emAndamento ??= api
    .post('/Verificacao')
    .then(() => undefined)
    .finally(() => {
      emAndamento = null
    })
  return emAndamento
}
