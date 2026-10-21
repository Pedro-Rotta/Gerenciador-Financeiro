/**
 * Tema da interface: claro, escuro ou igual ao sistema operacional.
 * O modo fica no localStorage ('tema') e é o mesmo para todas as telas: o botão de sol/lua
 * do topo e a aba Preferências das Configurações mexem no mesmo estado.
 * Aplica o atributo data-theme no <html> quando o painel abre; no modo 'sistema' acompanha o
 * sistema ao vivo. A tela de login fica sempre clara (o logout volta o <html> para claro).
 */

import { useCallback, useEffect, useSyncExternalStore } from 'react'

type Tema = 'light' | 'dark'
export type ModoTema = Tema | 'sistema'

const consulta = window.matchMedia?.('(prefers-color-scheme: dark)')
const ouvintes = new Set<() => void>()

function lerModo(): ModoTema {
  const salvo = localStorage.getItem('tema')
  return salvo === 'dark' || salvo === 'light' ? salvo : 'sistema'
}

let modo: ModoTema = lerModo()

function temaDoModo(m: ModoTema): Tema {
  if (m !== 'sistema') return m
  return consulta?.matches ? 'dark' : 'light'
}

let tema: Tema = temaDoModo(modo)

function aplicar() {
  tema = temaDoModo(modo)
  document.documentElement.setAttribute('data-theme', tema)
  ouvintes.forEach((ouvir) => ouvir())
}

// Só com o painel aberto (alguém usando o hook): a tela de login fica sempre clara
consulta?.addEventListener('change', () => {
  if (modo === 'sistema' && ouvintes.size > 0) aplicar()
})

function definirModo(novo: ModoTema) {
  modo = novo
  localStorage.setItem('tema', novo)
  aplicar()
}

function inscrever(ouvir: () => void) {
  ouvintes.add(ouvir)
  return () => {
    ouvintes.delete(ouvir)
  }
}

export function useTema() {
  // Ao abrir o painel (depois do login), aplica o tema salvo neste navegador
  useEffect(() => {
    aplicar()
  }, [])

  const temaAtual = useSyncExternalStore(inscrever, () => tema)
  const modoAtual = useSyncExternalStore(inscrever, () => modo)

  // Botão do topo: troca para o oposto do que está na tela (sai do modo "sistema")
  const alternarTema = useCallback(() => {
    definirModo(tema === 'dark' ? 'light' : 'dark')
  }, [])

  return { tema: temaAtual, modo: modoAtual, alternarTema, definirModo }
}
