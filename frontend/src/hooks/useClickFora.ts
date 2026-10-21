/**
 * Hook utilitário para interações visuais.
 * Fecha um contêiner quando o usuário clica fora dele.
 */

import { useEffect, type RefObject } from 'react'

export function useClickFora(
  ref: RefObject<HTMLElement | null>,
  aberto: boolean,
  aoFechar: () => void,
) {
  useEffect(() => {
    if (!aberto) return

    function tratarClique(evento: MouseEvent) {
      const alvo = evento.target as Node
      if (ref.current && !ref.current.contains(alvo)) {
        aoFechar()
      }
    }

    function tratarTecla(evento: KeyboardEvent) {
      if (evento.key === 'Escape') aoFechar()
    }

    document.addEventListener('mousedown', tratarClique)
    document.addEventListener('keydown', tratarTecla)
    return () => {
      document.removeEventListener('mousedown', tratarClique)
      document.removeEventListener('keydown', tratarTecla)
    }
  }, [ref, aberto, aoFechar])
}
