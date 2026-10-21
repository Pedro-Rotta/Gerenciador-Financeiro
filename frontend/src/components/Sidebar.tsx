/**
 * Sidebar — Navegação principal (desktop).
 * Contém logo/branding e itens de navegação com ícone + label.
 * O item ativo é controlado via props (state no Layout) para permitir
 * a alternância visual entre as seções sem depender de backend.
 */

import type { ReactNode } from 'react'
import {
  IconeTransacoes,
  IconeContas,
  IconeCategorias,
  IconeRelatorios,
  IconeMeta,
  IconeCarteira,
  IconeAlvo,
  IconeUsuarios,
} from './icons'

export type Secao = 'transacoes' | 'contas' | 'contasConjuntas' | 'categorias' | 'metas' | 'relatorios'

interface ItemNav {
  id: Secao
  label: string
  icone: ReactNode
}

const ITENS: ItemNav[] = [
  { id: 'transacoes', label: 'Transações', icone: <IconeTransacoes /> },
  { id: 'contas', label: 'Contas', icone: <IconeContas /> },
  { id: 'contasConjuntas', label: 'Contas Conjuntas', icone: <IconeUsuarios /> },
  { id: 'categorias', label: 'Categorias', icone: <IconeCategorias /> },
  { id: 'metas', label: 'Metas', icone: <IconeAlvo /> },
  { id: 'relatorios', label: 'Relatórios', icone: <IconeRelatorios /> },
]

interface SidebarProps {
  secaoAtiva: Secao | null
  aberto: boolean
  aoSelecionar: (secao: Secao) => void
  aoFechar: () => void
}

function Logo() {
  return (
    <div className="sidebar-logo">
      <div className="sidebar-logo-icone">
        <IconeCarteira />
      </div>
      <div>
        <div className="sidebar-logo-nome">Finança</div>
        <div className="sidebar-logo-sub">Gerenciador Familiar</div>
      </div>
    </div>
  )
}

function NavLinks({ secaoAtiva, aoSelecionar }: Omit<SidebarProps, 'aberto' | 'aoFechar'>) {
  return (
    <nav className="sidebar-nav" aria-label="Navegação principal">
      <div className="sidebar-label">Menu</div>
      {ITENS.map((item) => (
        <button
          key={item.id}
          type="button"
          onClick={() => aoSelecionar(item.id)}
          className={`sidebar-item ${secaoAtiva === item.id ? 'ativo' : ''}`}
          aria-current={secaoAtiva === item.id ? 'page' : undefined}
        >
          {item.icone}
          <span>{item.label}</span>
        </button>
      ))}
    </nav>
  )
}

export default function Sidebar({ secaoAtiva, aberto, aoSelecionar, aoFechar }: SidebarProps) {
  return (
    <div className={`sidebar ${aberto ? 'aberto' : ''}`}>
      <Logo />
      <NavLinks secaoAtiva={secaoAtiva} aoSelecionar={aoSelecionar} />
      <div className="sidebar-rodape">
        <NavLinksRodape aoFechar={aoFechar} />
      </div>
    </div>
  )
}

/* Rodapé da sidebar — apenas decoração visual (versão, ajuda). */
function NavLinksRodape({ aoFechar }: { aoFechar: () => void }) {
  return (
    <nav className="sidebar-nav" aria-label="Ajuda">
      <button type="button" onClick={aoFechar} className="sidebar-item">
        <IconeMeta />
        <span>Central de ajuda</span>
      </button>
    </nav>
  )
}
