/**
 * Modal — Caixa de diálogo reutilizável.
 * Fundo escuro translúcido + painel central arredondado, seguindo o design
 * system do site. Fecha ao clicar no overlay, no botão X ou com Escape.
 */

import { useEffect, type ReactNode } from 'react'
import { IconeFechar } from './icons'

interface ModalProps {
  aberto: boolean
  titulo: string
  subtitulo?: string
  aoFechar: () => void
  children: ReactNode
  largura?: 'sm' | 'md' | 'lg'
}

export default function Modal({
  aberto,
  titulo,
  subtitulo,
  aoFechar,
  children,
  largura = 'sm',
}: ModalProps) {
  // Fecha com a tecla Escape
  useEffect(() => {
    if (!aberto) return
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') aoFechar()
    }
    document.addEventListener('keydown', handler)
    return () => document.removeEventListener('keydown', handler)
  }, [aberto, aoFechar])

  if (!aberto) return null

  return (
    <div className="modal-overlay" onClick={aoFechar}>
      <div
        className={`modal-painel modal-${largura}`}
        role="dialog"
        aria-modal="true"
        aria-label={titulo}
        onClick={(e) => e.stopPropagation()}
      >
        <div className="modal-cabecalho">
          <div>
            <h2 className="modal-titulo">{titulo}</h2>
            {subtitulo && <p className="modal-sub">{subtitulo}</p>}
          </div>
          <button type="button" className="modal-fechar" aria-label="Fechar" onClick={aoFechar}>
            <IconeFechar />
          </button>
        </div>
        {children}
      </div>
    </div>
  )
}
