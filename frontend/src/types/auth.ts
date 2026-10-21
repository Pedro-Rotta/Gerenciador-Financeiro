/**
 * Tipos de Autenticação, alinhados às DTOs do backend (.NET).
 */

export interface LoginResponse {
  membroId: number
  nome: string
  nomeDeUsuario: string
  email: string
  expiraEm: string
}
