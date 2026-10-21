/**
 * MobileNav — Barra de navegação inferior (bottom navigation).
 * Visível apenas em telas pequenas (<= 640px) via CSS.
 * Replica a navegação principal permitindo alternar a seção ativa.
 */

import type { ReactNode } from 'react'
import type { Secao } from './Sidebar'
import {
  IconeTransacoes,
  IconeContas,
  IconeCategorias,
  IconeRelatorios,
  IconeAlvo,
  IconeUsuarios,
} from './icons'

interface ItemBottomNav {
  id: Secao
  label: string
  icone: ReactNode
}

const ITENS: ItemBottomNav[] = [
  { id: 'transacoes', label: 'Transações', icone: <IconeTransacoes /> },
  { id: 'contas', label: 'Contas', icone: <IconeContas /> },
  { id: 'contasConjuntas', label: 'Conjuntas', icone: <IconeUsuarios /> },
  { id: 'categorias', label: 'Categorias', icone: <IconeCategorias /> },
  { id: 'metas', label: 'Metas', icone: <IconeAlvo /> },
  { id: 'relatorios', label: 'Relatórios', icone: <IconeRelatorios /> },
]

interface MobileNavProps {
  secaoAtiva: Secao | null
  aoSelecionar: (secao: Secao) => void
}

export default function MobileNav({ secaoAtiva, aoSelecionar }: MobileNavProps) {
  return (
    <nav className="bottom-nav" aria-label="Navegação inferior">
      {ITENS.map((item) => (
        <button
          key={item.id}
          type="button"
          onClick={() => aoSelecionar(item.id)}
          className={`bottom-nav-item ${secaoAtiva === item.id ? 'ativo' : ''}`}
          aria-current={secaoAtiva === item.id ? 'page' : undefined}
        >
          {item.icone}
          <span>{item.label}</span>
        </button>
      ))}
    </nav>
  )
}
