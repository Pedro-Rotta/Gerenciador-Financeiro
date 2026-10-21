/**
 * ModalNotificacoes — "Ver todas as notificações" (aberto pelo sino da TopBar).
 * Lista o histórico completo: notificações lidas e não lidas (com filtro) e os
 * convites ainda pendentes. O sino mostra só o que é novo; aqui fica tudo.
 */

import { useEffect, useState } from 'react'
import Modal from './Modal'
import { IconeCarteira, IconeMeta, IconeSino, IconeTransacoes, IconeUsuarios } from './icons'
import { tempoRelativo } from '../utils/data'
import type { ConviteResponse } from '../types/convite'
import { GravidadeNotificacao, TipoNotificacao, type NotificacaoResponse } from '../types/notificacao'

type FiltroNotificacoes = 'todas' | 'nao-lidas' | 'lidas'

/* ---------- Ícone da notificação (usado no sino e no modal) ---------- */

// O tipo define o ícone e a gravidade a cor; só o ícone é colorido, a linha continua neutra
const ICONE_POR_TIPO: Record<TipoNotificacao, typeof IconeSino> = {
  [TipoNotificacao.GERAL]: IconeSino,
  [TipoNotificacao.CONTA_CONJUNTA]: IconeUsuarios,
  [TipoNotificacao.TRANSACAO]: IconeTransacoes,
  [TipoNotificacao.SALDO]: IconeCarteira,
  [TipoNotificacao.META]: IconeMeta,
}

const COR_POR_GRAVIDADE: Record<GravidadeNotificacao, string> = {
  [GravidadeNotificacao.INFORMATIVA]: 'verde',
  [GravidadeNotificacao.ATENCAO]: 'ambar',
  [GravidadeNotificacao.CRITICA]: 'vermelho',
}

export function IconeNotificacao({ notificacao }: { notificacao: NotificacaoResponse }) {
  // Valores desconhecidos (API mais nova que o front) caem no sino verde
  const Icone = ICONE_POR_TIPO[notificacao.tipo] ?? IconeSino
  const cor = COR_POR_GRAVIDADE[notificacao.gravidade] ?? 'verde'
  return (
    <span className={`notificacao-icone ${cor}`}>
      <Icone />
    </span>
  )
}

/* ---------- Convite pendente (usado no sino e no modal) ---------- */

interface ItemConviteProps {
  convite: ConviteResponse
  respondendo: boolean
  aoResponder: (convite: ConviteResponse, aceitar: boolean) => void
}

export function ItemConvite({ convite, respondendo, aoResponder }: ItemConviteProps) {
  return (
    <div className="notificacao">
      <span className="notificacao-icone azul">
        <IconeUsuarios />
      </span>
      <div className="notificacao-texto">
        <div className="notificacao-titulo">Convite para conta conjunta</div>
        <div className="notificacao-desc">
          {convite.convidadoPorNome} convidou você para participar de "{convite.contaConjuntaNome}".
        </div>
        <div className="notificacao-tempo">{tempoRelativo(convite.criadoEm)}</div>
        <div className="notificacao-acoes">
          <button
            type="button"
            className="btn btn-primario"
            disabled={respondendo}
            onClick={() => aoResponder(convite, true)}
          >
            Aceitar
          </button>
          <button
            type="button"
            className="btn btn-ghost"
            disabled={respondendo}
            onClick={() => aoResponder(convite, false)}
          >
            Recusar
          </button>
        </div>
      </div>
    </div>
  )
}

/* ---------- Modal com todas as notificações ---------- */

interface ModalNotificacoesProps {
  aberto: boolean
  aoFechar: () => void
  notificacoes: NotificacaoResponse[]
  convites: ConviteResponse[]
  respondendoId: number | null
  erro: string
  aoMarcarLida: (id: number) => void
  aoMarcarTodasLidas: () => void
  aoResponderConvite: (convite: ConviteResponse, aceitar: boolean) => void
}

function dataCompleta(dataIso: string) {
  return new Date(dataIso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

export default function ModalNotificacoes({
  aberto,
  aoFechar,
  notificacoes,
  convites,
  respondendoId,
  erro,
  aoMarcarLida,
  aoMarcarTodasLidas,
  aoResponderConvite,
}: ModalNotificacoesProps) {
  const [filtro, setFiltro] = useState<FiltroNotificacoes>('todas')

  // Sempre abre mostrando todas
  useEffect(() => {
    if (aberto) setFiltro('todas')
  }, [aberto])

  const naoLidas = notificacoes.filter((n) => !n.lida)
  const lidas = notificacoes.filter((n) => n.lida)
  const visiveis = filtro === 'nao-lidas' ? naoLidas : filtro === 'lidas' ? lidas : notificacoes
  // Convites pendentes também são "não lidos": aparecem em Todas e em Não lidas
  const convitesVisiveis = filtro === 'lidas' ? [] : convites

  const opcoes: [FiltroNotificacoes, string, number][] = [
    ['todas', 'Todas', notificacoes.length + convites.length],
    ['nao-lidas', 'Não lidas', naoLidas.length + convites.length],
    ['lidas', 'Lidas', lidas.length],
  ]

  return (
    <Modal
      aberto={aberto}
      titulo="Todas as notificações"
      aoFechar={aoFechar}
      largura="md"
    >
      <div className="notificacoes-todas-barra">
        <div className="btn-grupo" role="group" aria-label="Filtrar notificações">
          {opcoes.map(([valor, rotulo, quantidade]) => (
            <button
              key={valor}
              type="button"
              className={filtro === valor ? 'ativo' : ''}
              aria-pressed={filtro === valor}
              onClick={() => setFiltro(valor)}
            >
              {rotulo} ({quantidade})
            </button>
          ))}
        </div>
        {naoLidas.length > 0 && (
          <button type="button" className="btn-link" onClick={aoMarcarTodasLidas}>
            Marcar todas como lidas
          </button>
        )}
      </div>

      {erro && <p className="erro">{erro}</p>}

      <div className="notificacoes-todas-lista">
        {visiveis.length === 0 && convitesVisiveis.length === 0 && (
          <div className="estado-vazio">
            {filtro === 'nao-lidas'
              ? 'Nenhuma notificação não lida.'
              : filtro === 'lidas'
                ? 'Nenhuma notificação lida.'
                : 'Nenhuma notificação.'}
          </div>
        )}

        {convitesVisiveis.map((convite) => (
          <ItemConvite
            key={`convite-${convite.id}`}
            convite={convite}
            respondendo={respondendoId === convite.id}
            aoResponder={aoResponderConvite}
          />
        ))}

        {visiveis.map((notif) => (
          <div key={`notificacao-${notif.id}`} className={`notificacao ${notif.lida ? 'lida' : ''}`}>
            <IconeNotificacao notificacao={notif} />
            <div className="notificacao-texto">
              <div className="notificacao-titulo">{notif.mensagem}</div>
              <div className="notificacao-tempo" title={dataCompleta(notif.criadoEm)}>
                {dataCompleta(notif.criadoEm)} · {tempoRelativo(notif.criadoEm)}
              </div>
            </div>
            <div className="notificacao-lateral">
              {notif.lida ? (
                <span className="tag fosco">Lida</span>
              ) : (
                <>
                  <span className="tag azul">Não lida</span>
                  <button type="button" className="btn-link" onClick={() => aoMarcarLida(notif.id)}>
                    Marcar como lida
                  </button>
                </>
              )}
            </div>
          </div>
        ))}
      </div>
    </Modal>
  )
}
