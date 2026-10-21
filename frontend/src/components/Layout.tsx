/**
 * Layout — Shell da aplicação.
 * Estrutura de grade: [Sidebar | TopBar ; Sidebar | Main].
 * Gerencia:
 *   - Navegação ativa (seção selecionada)
 *   - Menu mobile (drawer off-canvas) + overlay
 *   - Tema claro/escuro
 *   - Preferências de uso do membro (carregadas antes de mostrar as páginas)
 * E envolve o conteúdo dinâmico (Outlet) do React Router.
 */

import { useEffect, useState } from 'react'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import Sidebar, { type Secao } from './Sidebar'
import MobileNav from './MobileNav'
import TopBar from './TopBar'
import { useTema } from '../hooks/useTema'
import { obterPreferenciasUso } from '../services/membros'
import { guardarPreferenciasUso, PREFERENCIAS_PADRAO } from '../utils/preferenciasUso'

// Mapeia cada seção para sua rota
const ROTAS: Record<Secao, string> = {
  transacoes: '/dashboard',
  contas: '/contas',
  contasConjuntas: '/contas-conjuntas',
  categorias: '/categorias',
  metas: '/metas',
  relatorios: '/relatorios',
}

// Seção do menu para a URL; null em telas fora do menu (ex.: Configurações),
// para nenhum item ficar destacado
function rotaParaSecao(pathname: string): Secao | null {
  const entrada = Object.entries(ROTAS).find(([, rota]) => rota === pathname)
  return (entrada?.[0] as Secao) ?? null
}

export default function Layout() {
  const [menuAberto, setMenuAberto] = useState(false)
  const { tema, alternarTema } = useTema()
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const [preferenciasProntas, setPreferenciasProntas] = useState(false)

  // As páginas leem as preferências de uso (semana, conta padrão, período, ocultar valores)
  // na hora de abrir; sem resposta da API, valem os padrões
  useEffect(() => {
    let cancelado = false
    obterPreferenciasUso()
      .then(guardarPreferenciasUso)
      .catch(() => guardarPreferenciasUso(PREFERENCIAS_PADRAO))
      .finally(() => {
        if (!cancelado) setPreferenciasProntas(true)
      })
    return () => {
      cancelado = true
    }
  }, [])

  // A seção ativa é derivada da URL (fonte de verdade)
  const secaoAtiva = rotaParaSecao(pathname)

  const selecionarSeca = (secao: Secao) => {
    setMenuAberto(false)
    navigate(ROTAS[secao])
  }

  return (
    <div className="app-layout">
      {/* Overlay para o drawer mobile */}
      <div
        className={`sidebar-overlay ${menuAberto ? 'aberto' : ''}`}
        onClick={() => setMenuAberto(false)}
      />

      <Sidebar
        secaoAtiva={secaoAtiva}
        aberto={menuAberto}
        aoSelecionar={selecionarSeca}
        aoFechar={() => setMenuAberto(false)}
      />

      <TopBar
        menuAberto={menuAberto}
        aoAlternarMenu={() => setMenuAberto((v) => !v)}
        tema={tema}
        aoAlternarTema={alternarTema}
      />

      <main className="main">
        {preferenciasProntas ? (
          <Outlet context={{ secaoAtiva }} />
        ) : (
          <div className="estado-vazio">Carregando...</div>
        )}
      </main>

      <MobileNav secaoAtiva={secaoAtiva} aoSelecionar={selecionarSeca} />
    </div>
  )
}
