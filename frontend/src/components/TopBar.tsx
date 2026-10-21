/**
 * TopBar — Barra superior.
 * Contém: botão de menu (mobile), busca global, toggle de tema,
 * menu de notificações (com badge de não lidas) e menu de perfil.
 * O sino mostra só o que é novo: convites pendentes para contas conjuntas (com
 * Aceitar/Recusar) e notificações não lidas — ao marcar como lida, ela sai do sino.
 * "Ver todas as notificações" abre o histórico completo (lidas e não lidas).
 * Os menus dropdown fecham ao clicar fora (useClickFora) ou com Escape.
 */

import { useCallback, useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import {
  IconeBusca,
  IconeSino,
  IconeSol,
  IconeLua,
  IconeChevronBaixo,
  IconeMenu,
  IconeConfig,
  IconeSair,
} from './icons'
import ModalNotificacoes, { IconeNotificacao, ItemConvite } from './ModalNotificacoes'
import { useClickFora } from '../hooks/useClickFora'
import { tempoRelativo } from '../utils/data'
import {
  listarConvitesRecebidos,
  aceitarConvite,
  recusarConvite,
  EVENTO_CONTAS_CONJUNTAS_ALTERADAS,
} from '../services/convites'
import { listarNotificacoes, marcarNotificacaoComoLida } from '../services/notificacoes'
import { executarVerificacao } from '../services/verificacao'
import type { ConviteResponse } from '../types/convite'
import type { NotificacaoResponse } from '../types/notificacao'
import { extrairErro } from '../utils/erros'
import { mostrarNoNavegador } from '../utils/notificacoesNavegador'

// Intervalo para buscar novos convites/notificações enquanto o usuário está logado
const INTERVALO_ATUALIZACAO_MS = 60_000

interface TopBarProps {
  menuAberto: boolean
  aoAlternarMenu: () => void
  tema: 'light' | 'dark'
  aoAlternarTema: () => void
}

export default function TopBar({
  menuAberto,
  aoAlternarMenu,
  tema,
  aoAlternarTema,
}: TopBarProps) {
  const { nome, email, logout } = useAuth()
  const navigate = useNavigate()
  const [busca, setBusca] = useState('')
  const [notifAberto, setNotifAberto] = useState(false)
  const [perfilAberto, setPerfilAberto] = useState(false)

  // Itens do menu de perfil: abrem a tela de Configurações na aba certa
  const abrirConfiguracoes = (aba?: string) => {
    setPerfilAberto(false)
    navigate(aba ? `/configuracoes?aba=${aba}` : '/configuracoes')
  }
  const [convites, setConvites] = useState<ConviteResponse[]>([])
  const [notificacoes, setNotificacoes] = useState<NotificacaoResponse[]>([])
  const [respondendoId, setRespondendoId] = useState<number | null>(null)
  const [erroNotif, setErroNotif] = useState('')
  const [todasAbertas, setTodasAbertas] = useState(false)

  const refNotif = useRef<HTMLDivElement>(null)
  const refPerfil = useRef<HTMLDivElement>(null)

  useClickFora(refNotif, notifAberto, () => setNotifAberto(false))
  useClickFora(refPerfil, perfilAberto, () => setPerfilAberto(false))

  // Notificações do navegador: só as que chegam depois de abrir o app (as não lidas da
  // primeira carga ficam marcadas como vistas, para não repetir tudo a cada abertura)
  const vistasRef = useRef<Set<number> | null>(null)
  const avisarNoNavegador = useCallback((lista: NotificacaoResponse[]) => {
    const naoLidas = lista.filter((n) => !n.lida)
    if (vistasRef.current === null) {
      vistasRef.current = new Set(naoLidas.map((n) => n.id))
      return
    }
    const vistas = vistasRef.current
    const novas = naoLidas.filter((n) => !vistas.has(n.id))
    novas.forEach((n) => vistas.add(n.id))
    void mostrarNoNavegador(novas)
  }, [])

  // Busca convites pendentes e notificações (falhas não interrompem a tela).
  // Antes, dispara a verificação (gera ocorrências recorrentes etc.; o servidor limita a 1×/hora),
  // para que os avisos dela já apareçam no sino.
  const carregarNotificacoes = useCallback(async () => {
    try {
      await executarVerificacao()
    } catch {
      // a verificação é opcional: o sino carrega mesmo se ela falhar
    }
    try {
      const [convitesData, notificacoesData] = await Promise.all([
        listarConvitesRecebidos(),
        listarNotificacoes(),
      ])
      setConvites(convitesData)
      setNotificacoes(notificacoesData)
      avisarNoNavegador(notificacoesData)
    } catch {
      // mantém o que já estava na tela
    }
  }, [avisarNoNavegador])

  // Carrega ao montar e atualiza periodicamente
  useEffect(() => {
    carregarNotificacoes()
    const timer = window.setInterval(carregarNotificacoes, INTERVALO_ATUALIZACAO_MS)
    return () => window.clearInterval(timer)
  }, [carregarNotificacoes])

  // O sino mostra só as não lidas; as lidas ficam no histórico ("Ver todas")
  const notificacoesNaoLidas = notificacoes.filter((n) => !n.lida)
  const naoLidas = convites.length + notificacoesNaoLidas.length
  const inicial = (nome ?? 'U').charAt(0).toUpperCase()

  const abrirNotificacoes = () => {
    setNotifAberto((v) => !v)
    setErroNotif('')
    if (!notifAberto) carregarNotificacoes()
  }

  const abrirTodas = () => {
    setNotifAberto(false)
    setErroNotif('')
    setTodasAbertas(true)
    carregarNotificacoes()
  }

  // Marca uma notificação como lida: some do sino na hora e continua no histórico
  const marcarComoLida = async (id: number) => {
    setNotificacoes((prev) => prev.map((n) => (n.id === id ? { ...n, lida: true } : n)))
    try {
      await marcarNotificacaoComoLida(id)
    } catch {
      carregarNotificacoes()
    }
  }

  const marcarTodasLidas = async () => {
    const pendentes = notificacoes.filter((n) => !n.lida)
    setNotificacoes((prev) => prev.map((n) => ({ ...n, lida: true })))
    try {
      await Promise.all(pendentes.map((n) => marcarNotificacaoComoLida(n.id)))
    } catch {
      carregarNotificacoes()
    }
  }

  // Aceita ou recusa um convite para conta conjunta
  const responderConvite = async (convite: ConviteResponse, aceitar: boolean) => {
    setRespondendoId(convite.id)
    setErroNotif('')
    try {
      if (aceitar) {
        await aceitarConvite(convite.id)
        // Avisa as telas abertas (ex.: Contas Conjuntas) para recarregar
        window.dispatchEvent(new Event(EVENTO_CONTAS_CONJUNTAS_ALTERADAS))
      } else {
        await recusarConvite(convite.id)
      }
      setConvites((prev) => prev.filter((c) => c.id !== convite.id))
    } catch (err: unknown) {
      setErroNotif(extrairErro(err, 'Não foi possível responder o convite.'))
      carregarNotificacoes()
    } finally {
      setRespondendoId(null)
    }
  }

  return (
    <header className="topbar">
      <button
        className="topbar-menu-btn"
        type="button"
        onClick={aoAlternarMenu}
        aria-label={menuAberto ? 'Fechar menu' : 'Abrir menu'}
        aria-expanded={menuAberto}
      >
        <IconeMenu />
      </button>

      {/* Busca global */}
      <div className="busca">
        <IconeBusca className="busca-icone" />
        <input
          type="text"
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          placeholder="Buscar transações, contas..."
          aria-label="Busca global"
        />
      </div>

      <div className="topbar-acoes">
        {/* Toggle de tema */}
        <button
          className="icon-btn"
          type="button"
          onClick={aoAlternarTema}
          aria-label={tema === 'dark' ? 'Ativar tema claro' : 'Ativar tema escuro'}
        >
          {tema === 'dark' ? <IconeSol /> : <IconeLua />}
        </button>

        {/* Menu de notificações */}
        <div className="dropdown" ref={refNotif}>
          <button
            className="icon-btn"
            type="button"
            onClick={abrirNotificacoes}
            aria-label="Notificações"
            aria-expanded={notifAberto}
          >
            <IconeSino />
            {naoLidas > 0 && <span className="badge">{naoLidas}</span>}
          </button>

          <div className={`dropdown-painel ${notifAberto ? 'aberto' : ''}`}>
            <div className="dropdown-cabecalho">
              <h3>Notificações</h3>
              {notificacoesNaoLidas.length > 0 && (
                <button type="button" className="btn-link" onClick={marcarTodasLidas}>
                  Marcar todas como lidas
                </button>
              )}
            </div>
            <div className="dropdown-lista">
              {erroNotif && <p className="erro">{erroNotif}</p>}

              {convites.length === 0 && notificacoesNaoLidas.length === 0 && (
                <div className="estado-vazio">Nenhuma notificação nova.</div>
              )}

              {/* Convites pendentes para contas conjuntas */}
              {convites.map((convite) => (
                <ItemConvite
                  key={`convite-${convite.id}`}
                  convite={convite}
                  respondendo={respondendoId === convite.id}
                  aoResponder={responderConvite}
                />
              ))}

              {/* Notificações não lidas (marcar como lida tira do sino) */}
              {notificacoesNaoLidas.map((notif) => (
                <div key={`notificacao-${notif.id}`} className="notificacao">
                  <IconeNotificacao notificacao={notif} />
                  <div className="notificacao-texto">
                    <div className="notificacao-titulo">{notif.mensagem}</div>
                    <div className="notificacao-tempo">{tempoRelativo(notif.criadoEm)}</div>
                    <div className="notificacao-acoes">
                      <button type="button" className="btn-link" onClick={() => marcarComoLida(notif.id)}>
                        Marcar como lida
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
            <div className="dropdown-rodape">
              <button type="button" className="menu-item" onClick={abrirTodas}>
                <IconeConfig />
                Ver todas as notificações
              </button>
            </div>
          </div>
        </div>

        {/* Menu de perfil */}
        <div className="dropdown" ref={refPerfil}>
          <button
            className="perfil-btn"
            type="button"
            onClick={() => setPerfilAberto((v) => !v)}
            aria-expanded={perfilAberto}
          >
            <span className="avatar">{inicial}</span>
            <span className="perfil-btn-info">
              <span className="perfil-btn-nome">{nome ?? 'Usuário'}</span>
              <span className="perfil-btn-cargo">Cliente</span>
            </span>
            <IconeChevronBaixo />
          </button>

          <div className={`dropdown-painel ${perfilAberto ? 'aberto' : ''}`}>
            <div className="dropdown-cabecalho">
              <div>
                <h3>{nome ?? 'Usuário'}</h3>
                {email && (
                  <p className="painel-sub" style={{ margin: 0 }}>
                    {email}
                  </p>
                )}
              </div>
            </div>
            <div className="dropdown-lista">
              <button type="button" className="menu-item" onClick={() => abrirConfiguracoes()}>
                <IconeConfig />
                Configurações
              </button>
              <button type="button" className="menu-item" onClick={() => abrirConfiguracoes('notificacoes')}>
                <IconeSino />
                Preferências de notificação
              </button>
            </div>
            <div className="dropdown-rodape">
              <button type="button" className="menu-item danger" onClick={logout}>
                <IconeSair />
                Sair da conta
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Portal no <body>: dentro da topbar (sticky, z-index próprio) o overlay
          ficaria preso nesse contexto de empilhamento, atrás do conteúdo da página */}
      {createPortal(
        <ModalNotificacoes
          aberto={todasAbertas}
          aoFechar={() => setTodasAbertas(false)}
          notificacoes={notificacoes}
          convites={convites}
          respondendoId={respondendoId}
          erro={erroNotif}
          aoMarcarLida={marcarComoLida}
          aoMarcarTodasLidas={marcarTodasLidas}
          aoResponderConvite={responderConvite}
        />,
        document.body,
      )}
    </header>
  )
}
