import { createContext } from 'react'

export interface AuthContextType {
  /** Há sessão aberta (o token fica no cookie HttpOnly, fora do alcance do JavaScript) */
  logado: boolean
  membroId: number | null
  nome: string | null
  /** E-mail do membro logado */
  email: string | null
  nomeDeUsuario: string | null
  login: (nomeDeUsuario: string, senha: string) => Promise<void>
  logout: () => void
  /** Sessão renovada (ex.: depois de alterar a senha): guarda a validade nova */
  renovarSessao: (expiraEm: string) => void
  /** Perfil editado nas Configurações: atualiza o que a tela mostra (topo, saudação) */
  atualizarPerfil: (nome: string, email: string, nomeDeUsuario: string) => void
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined)