/**
 * Tipos da entidade Membro, alinhados às DTOs do backend (.NET).
 */

/** POST api/Membro — cria um novo membro */
export interface CriarMembroRequest {
  nome: string
  nomeDeUsuario: string
  email: string
  senha: string
  /** Senha digitada de novo: precisa ser idêntica a `senha` */
  confirmacaoSenha: string
  telefone?: string
  dataNascimento?: string
}

/** GET/PUT api/Membro — perfil do membro logado */
export interface PerfilResponse {
  id: number
  nome: string
  nomeDeUsuario: string
  email: string
  telefone: string | null
  /** "yyyy-MM-dd" */
  dataNascimento: string | null
}

/** GET/PUT api/Membro/preferencias — preferências de uso do app (Configurações → Preferências) */
export interface PreferenciasUso {
  /** false = a semana começa na segunda-feira */
  semanaComecaNoDomingo: boolean
  /** "c:ID" ou "cc:ID"; null = a conta principal */
  contaPadrao: string | null
  /** Período padrão dos Relatórios, em meses: 1, 3, 6 ou 12 */
  periodoRelatorios: number
  /** Saldos do painel aparecem como •••• até o membro tocar para ver */
  ocultarValores: boolean
}

/** PUT api/Membro — campos do perfil (o membro vem da sessão) */
export interface EditarPerfilRequest {
  nome?: string
  nomeDeUsuario?: string
  email?: string
  telefone?: string | null
  dataNascimento?: string | null
}
