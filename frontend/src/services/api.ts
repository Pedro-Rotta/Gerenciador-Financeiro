import axios from 'axios'

/** Evento disparado quando a API recusa a sessão (expirada). Tratado pelo AuthProvider. */
export const EVENTO_SESSAO_EXPIRADA = 'sessao-expirada'

// A sessão vai no cookie HttpOnly gravado pela API no login: o navegador o envia
// sozinho em toda requisição para /api, e o JavaScript nunca vê o token.
const api = axios.create({
  baseURL: '/api',
})

api.interceptors.response.use(
  (resposta) => resposta,
  (erro: unknown) => {
    // 401 sem corpo = o JwtBearer recusou o cookie de sessão (expirado ou inválido).
    // 401 com { mensagem } vem do middleware do backend e não é sessão expirada.
    // Falta de permissão sobre um recurso chega como 403 e não passa por aqui.
    if (axios.isAxiosError(erro) && erro.response?.status === 401 && localStorage.getItem('membroId')) {
      const dados = erro.response.data as { mensagem?: string } | undefined
      if (!dados?.mensagem) {
        window.dispatchEvent(new Event(EVENTO_SESSAO_EXPIRADA))
      }
    }
    return Promise.reject(erro)
  },
)

export default api
