/**
 * Preferências de uso do membro logado (Configurações → Preferências), guardadas em memória
 * para as telas lerem sem outra chamada à API. O Layout carrega antes de mostrar as páginas;
 * a aba Preferências atualiza a cada mudança salva.
 */

import type { PreferenciasUso } from '../types/membro'

export const PREFERENCIAS_PADRAO: PreferenciasUso = {
  semanaComecaNoDomingo: false,
  contaPadrao: null,
  periodoRelatorios: 1,
  ocultarValores: false,
}

let atuais: PreferenciasUso = PREFERENCIAS_PADRAO

export function preferenciasUso(): PreferenciasUso {
  return atuais
}

export function guardarPreferenciasUso(preferencias: PreferenciasUso): void {
  atuais = preferencias
}
