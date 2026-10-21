import { useEffect, useState, type ReactNode } from 'react'
import { login as autenticar, logout as sairDaApi } from '../services/auth'
import { EVENTO_SESSAO_EXPIRADA } from '../services/api'
import SessaoExpiradaModal from '../components/SessaoExpiradaModal'
import { AuthContext } from './authContextDef'

// Maior atraso aceito pelo setTimeout (~24,8 dias)
const ATRASO_MAXIMO_MS = 2_147_483_647

// Chaves da sessão no localStorage (nenhuma é segredo: o token fica no cookie HttpOnly)
const CHAVES_DA_SESSAO = ['membroId', 'nome', 'email', 'nomeDeUsuario', 'expiraEm']

// Sessão de antes do cookie: o token antigo não vale mais (chave do JWT trocada).
// Limpa tudo para a pessoa logar de novo.
if (localStorage.getItem('token')) {
  localStorage.removeItem('token')
  CHAVES_DA_SESSAO.forEach((chave) => localStorage.removeItem(chave))
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [membroId, setMembroId] = useState<number | null>(() => {
    const id = localStorage.getItem('membroId')
    return id ? Number(id) : null
  })
  const [nome, setNome] = useState<string | null>(() => localStorage.getItem('nome'))
  const [email, setEmail] = useState<string | null>(() => localStorage.getItem('email'))
  const [nomeDeUsuario, setNomeDeUsuario] = useState<string | null>(() => localStorage.getItem('nomeDeUsuario'))
  const [expiraEm, setExpiraEm] = useState<string | null>(() => localStorage.getItem('expiraEm'))
  const [sessaoExpirada, setSessaoExpirada] = useState(false)
  const logado = membroId !== null

  // Avisa quando a sessão chega ao horário de expiração, mesmo sem nenhuma requisição.
  useEffect(() => {
    if (!logado || !expiraEm) return
    const restanteMs = new Date(expiraEm).getTime() - Date.now()
    if (Number.isNaN(restanteMs) || restanteMs > ATRASO_MAXIMO_MS) return
    const timer = window.setTimeout(() => setSessaoExpirada(true), Math.max(restanteMs, 0))
    return () => window.clearTimeout(timer)
  }, [logado, expiraEm])

  // A API recusou a sessão (ver interceptor em services/api.ts).
  useEffect(() => {
    const aoExpirar = () => setSessaoExpirada(true)
    window.addEventListener(EVENTO_SESSAO_EXPIRADA, aoExpirar)
    return () => window.removeEventListener(EVENTO_SESSAO_EXPIRADA, aoExpirar)
  }, [])

  const login = async (nomeDeUsuario: string, senha: string) => {
    const data = await autenticar(nomeDeUsuario, senha)

    localStorage.setItem('membroId', String(data.membroId))
    localStorage.setItem('nome', data.nome)
    localStorage.setItem('email', data.email)
    localStorage.setItem('nomeDeUsuario', data.nomeDeUsuario)
    localStorage.setItem('expiraEm', data.expiraEm)
    setMembroId(data.membroId)
    setNome(data.nome)
    setEmail(data.email)
    setNomeDeUsuario(data.nomeDeUsuario)
    setExpiraEm(data.expiraEm)
    setSessaoExpirada(false)
  }

  const renovarSessao = (novaExpiracao: string) => {
    localStorage.setItem('expiraEm', novaExpiracao)
    setExpiraEm(novaExpiracao)
    setSessaoExpirada(false)
  }

  const atualizarPerfil = (novoNome: string, novoEmail: string, novoUsuario: string) => {
    localStorage.setItem('nome', novoNome)
    localStorage.setItem('email', novoEmail)
    localStorage.setItem('nomeDeUsuario', novoUsuario)
    setNome(novoNome)
    setEmail(novoEmail)
    setNomeDeUsuario(novoUsuario)
  }

  const logout = () => {
    // Apaga o cookie na API; sem rede, sai do mesmo jeito (o cookie expira sozinho)
    sairDaApi().catch(() => {})
    CHAVES_DA_SESSAO.forEach((chave) => localStorage.removeItem(chave))
    setMembroId(null)
    setNome(null)
    setEmail(null)
    setNomeDeUsuario(null)
    setExpiraEm(null)
    setSessaoExpirada(false)

    // Ao sair, a tela de login/cadastro volta ao tema claro (tem aparência própria e não deve
    // herdar o dark mode do painel). O tema escolhido continua salvo e volta ao entrar.
    document.documentElement.setAttribute('data-theme', 'light')
  }

  return (
    <AuthContext.Provider value={{ logado, membroId, nome, email, nomeDeUsuario, login, logout, renovarSessao, atualizarPerfil }}>
      {children}
      {/* Ao fechar o aviso, encerra a sessão; o ProtectedRoute leva para o login. */}
      <SessaoExpiradaModal aberto={sessaoExpirada && logado} aoFechar={logout} />
    </AuthContext.Provider>
  )
}
