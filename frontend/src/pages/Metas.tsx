/**
 * Metas — Seção de Metas Financeiras (Goals & Savings).
 * Integrada ao backend: GET (listar), POST (criar) e PUT (editar).
 * Cards de resumo e grade de metas derivados dos dados reais.
 */

import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type FormEvent,
  type ReactNode,
} from 'react'
import {
  IconeMais,
  IconeAlvo,
  IconeCarteira,
  IconeSetaCima,
  IconeEllipsisVertical,
  IconeLapis,
  IconeLixeira,
} from '../components/icons'
import { useClickFora } from '../hooks/useClickFora'
import Modal from '../components/Modal'
import { criarMeta, editarMeta, excluirMeta, listarMetas } from '../services/metas'
import type { CriarMetaRequest, EditarMetaRequest, MetaResponse } from '../types/meta'
import { dataLocalISO } from '../utils/data'
import { extrairErro } from '../utils/erros'
/* ---------- Tipos ---------- */

type Prazo = 'Curto Prazo' | 'Médio Prazo' | 'Longo Prazo'
type CorIcone = 'indigo' | 'verde' | 'ambar' | 'info' | 'vermelho'

interface Meta {
  id: number
  nome: string
  icone: ReactNode
  cor: CorIcone
  prazo: Prazo
  prazoData: string
  valorAtual: number
  valorAlvo: number
  alvo: string
  status: 'ok' | 'atrasado'
}

/* ---------- Helpers ---------- */

const MESES = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']

function moeda(valor: number) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function progresso(meta: Meta) {
  return meta.valorAlvo > 0 ? Math.round((meta.valorAtual / meta.valorAlvo) * 100) : 0
}

function classePrazo(prazo: Prazo) {
  if (prazo === 'Curto Prazo') return 'curto'
  if (prazo === 'Médio Prazo') return 'medio'
  return 'longo'
}

function formatarAlvo(data: string): string {
  const [ano, mes] = data.split('-')
  const m = Number(mes)
  if (!ano || !m || Number.isNaN(m)) return data
  return `${MESES[m - 1]}/${ano}`
}

function classificarPrazo(data: string): Prazo {
  const alvo = new Date(`${data}T00:00:00`)
  const hoje = new Date()
  const diffMeses =
    (alvo.getFullYear() - hoje.getFullYear()) * 12 + (alvo.getMonth() - hoje.getMonth())
  if (diffMeses <= 6) return 'Curto Prazo'
  if (diffMeses <= 18) return 'Médio Prazo'
  return 'Longo Prazo'
}

function corDoPrazo(prazo: Prazo): CorIcone {
  if (prazo === 'Curto Prazo') return 'verde'
  if (prazo === 'Médio Prazo') return 'info'
  return 'indigo'
}

function metaAtrasada(resp: MetaResponse): boolean {
  if (resp.concluida) return false
  const hoje = new Date()
  hoje.setHours(0, 0, 0, 0)
  return new Date(`${resp.prazo}T00:00:00`) < hoje
}

function metaParaUi(resp: MetaResponse): Meta {
  const prazo = classificarPrazo(resp.prazo)
  return {
    id: resp.id,
    nome: resp.nome,
    icone: <IconeAlvo />,
    cor: corDoPrazo(prazo),
    prazo,
    prazoData: resp.prazo,
    valorAtual: resp.valorAtual,
    valorAlvo: resp.valorAlvo,
    alvo: formatarAlvo(resp.prazo),
    status: metaAtrasada(resp) ? 'atrasado' : 'ok',
  }
}

function aporteMensalRecomendado(metas: Meta[]): number {
  const hoje = new Date()
  hoje.setHours(0, 0, 0, 0)
  let total = 0
  for (const m of metas) {
    const restante = m.valorAlvo - m.valorAtual
    if (restante <= 0) continue
    const prazo = new Date(`${m.prazoData}T00:00:00`)
    if (prazo < hoje) continue
    const meses = Math.max(
      1,
      (prazo.getFullYear() - hoje.getFullYear()) * 12 + (prazo.getMonth() - hoje.getMonth()),
    )
    total += restante / meses
  }
  return Math.round(total * 100) / 100
}

/* ---------- Cards de resumo de objetivos ---------- */

interface ResumoMetaProps {
  rotulo: string
  valor: string
  sub: string
  icone: ReactNode
  cor: CorIcone
}

function ResumoMeta({ rotulo, valor, sub, icone, cor }: ResumoMetaProps) {
  return (
    <div className="card-resumo">
      <span className={`card-resumo-icone ${cor}`}>{icone}</span>
      <div className="card-resumo-info" style={{ width: '100%' }}>
        <div className="card-resumo-rotulo">{rotulo}</div>
        <div className="card-resumo-valor">{valor}</div>
        <div className="meta-resumo-sub">{sub}</div>
      </div>
    </div>
  )
}

/* ---------- Menu de três pontos do card ---------- */

interface MenuMetaProps {
  onEditar: () => void
  onExcluir: () => void
}

function MenuMeta({ onEditar, onExcluir }: MenuMetaProps) {
  const [aberto, setAberto] = useState(false)
  const ref = useRef<HTMLDivElement>(null)
  useClickFora(ref, aberto, () => setAberto(false))

  return (
    <div className="meta-menu" ref={ref}>
      <button
        type="button"
        className="meta-menu-btn"
        aria-label="Mais opções"
        onClick={() => setAberto((v) => !v)}
      >
        <IconeEllipsisVertical />
      </button>
      {aberto && (
        <div className="meta-menu-dropdown">
          <button
            type="button"
            className="meta-menu-item"
            onClick={() => {
              setAberto(false)
              onEditar()
            }}
          >
            Editar
          </button>
          <button
            type="button"
            className="meta-menu-item perigo"
            onClick={() => {
              setAberto(false)
              onExcluir()
            }}
          >
            Excluir
          </button>
        </div>
      )}
    </div>
  )
}

/* ---------- Card de meta ---------- */

function MetaCard({
  meta,
  onEditar,
  onExcluir,
}: {
  meta: Meta
  onEditar: (meta: Meta) => void
  onExcluir: (meta: Meta) => void
}) {
  const pct = progresso(meta)

  return (
    <article className="meta-card">
      <div className="meta-card-topo">
        <span className={`meta-card-icone ${meta.cor}`}>{meta.icone}</span>
        <span className={`meta-prazo ${classePrazo(meta.prazo)}`}>{meta.prazo}</span>
      </div>

      <div className="meta-card-nome">{meta.nome}</div>

      <div className="meta-card-valores">
        <strong>{moeda(meta.valorAtual)}</strong> de {moeda(meta.valorAlvo)}
        {meta.valorAtual > meta.valorAlvo && (
          <span className="meta-excedeu"> · excedeu {moeda(meta.valorAtual - meta.valorAlvo)}</span>
        )}
      </div>

      <div className="meta-progresso">
        <div className="meta-progresso-topo">
          <span>Progresso</span>
          <span className="meta-progresso-pct">{pct}%</span>
        </div>
        <div className="meta-progresso-barra">
          <div className="meta-progresso-preenchida" style={{ width: `${Math.min(pct, 100)}%` }} />
        </div>
      </div>

      <div className="meta-card-rodape">
        <span className="meta-card-alvo">
          Alvo: <strong>{meta.alvo}</strong>
        </span>
        <span className={`meta-status ${meta.status}`}>
          {meta.status === 'ok' ? 'No caminho certo' : 'Atrasado'}
        </span>
      </div>

      <div className="meta-card-acoes">
        <button type="button" className="btn btn-ghost meta-aportar">
          <IconeMais />
          <span>Aportar</span>
        </button>
        <MenuMeta onEditar={() => onEditar(meta)} onExcluir={() => onExcluir(meta)} />
      </div>
    </article>
  )
}

/* ---------- Modal de nova meta (POST) ---------- */

interface ModalNovaMetaProps {
  aberto: boolean
  aoFechar: () => void
  aoCriar: (meta: Meta) => void
}

function ModalNovaMeta({ aberto, aoFechar, aoCriar }: ModalNovaMetaProps) {
  const [nome, setNome] = useState('')
  const [valorAlvo, setValorAlvo] = useState('')
  const [prazo, setPrazo] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  useEffect(() => {
    if (aberto) {
      setNome('')
      setValorAlvo('')
      setPrazo('')
      setErro('')
    }
  }, [aberto])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErro('')
    setCarregando(true)
    try {
      const request: CriarMetaRequest = {
        nome: nome.trim(),
        valorAlvo: Number(valorAlvo),
        prazo,
      }
      const criada = await criarMeta(request)
      aoCriar(metaParaUi(criada))
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível criar a meta.'))
    } finally {
      setCarregando(false)
    }
  }

  const hoje = dataLocalISO()

  return (
    <Modal
      aberto={aberto}
      titulo="Nova Meta"
      subtitulo="Defina um objetivo financeiro e acompanhe seu progresso."
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Nome da Meta
          <input
            type="text"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            placeholder="Ex: Comprar Carro Novo"
            minLength={2}
            maxLength={100}
            required
            autoFocus
          />
        </label>

        <label className="campo">
          Valor Alvo
          <input
            type="number"
            value={valorAlvo}
            onChange={(e) => setValorAlvo(e.target.value)}
            placeholder="Ex: 30000"
            min="0.01"
            step="0.01"
            required
          />
        </label>

        <label className="campo">
          Prazo
          <input
            type="date"
            value={prazo}
            onChange={(e) => setPrazo(e.target.value)}
            min={hoje}
            required
          />
        </label>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primario" disabled={carregando}>
            {carregando ? 'Criando...' : 'Criar Meta'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

/* ---------- Modal de gerenciamento de metas (PUT) ---------- */

interface ModalGerenciarMetasProps {
  aberto: boolean
  aoFechar: () => void
  metas: Meta[]
  aoSalvar: (atualizada: MetaResponse) => void
  aoExcluir: (id: number) => void
}

function ModalGerenciarMetas({
  aberto,
  aoFechar,
  metas,
  aoSalvar,
  aoExcluir,
}: ModalGerenciarMetasProps) {
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [editandoNome, setEditandoNome] = useState('')
  const [editandoValorAlvo, setEditandoValorAlvo] = useState('')
  const [editandoPrazo, setEditandoPrazo] = useState('')
  const [editandoBtn, setEditandoBtn] = useState(false)
  const [excluindoId, setExcluindoId] = useState<number | null>(null)
  const [excluindoBtn, setExcluindoBtn] = useState(false)
  const [erro, setErro] = useState('')

  useEffect(() => {
    if (aberto) {
      setEditandoId(null)
      setExcluindoId(null)
      setErro('')
    }
  }, [aberto])

  const iniciarEdicao = (meta: Meta) => {
    setExcluindoId(null)
    setEditandoId(meta.id)
    setEditandoNome(meta.nome)
    setEditandoValorAlvo(String(meta.valorAlvo))
    setEditandoPrazo(meta.prazoData)
    setErro('')
  }

  const pedirExclusao = (id: number) => {
    setEditandoId(null)
    setExcluindoId((prev) => (prev === id ? null : id))
    setErro('')
  }

  const confirmarExclusao = async () => {
    if (excluindoId === null) return
    setErro('')
    setExcluindoBtn(true)
    try {
      await excluirMeta(excluindoId)
      aoExcluir(excluindoId)
      setExcluindoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível excluir a meta.'))
    } finally {
      setExcluindoBtn(false)
    }
  }

  const salvarEdicao = async () => {
    if (editandoId === null) return
    setErro('')
    setEditandoBtn(true)
    try {
      const request: EditarMetaRequest = {
        nome: editandoNome.trim() || null,
        valorAlvo: Number(editandoValorAlvo),
        prazo: editandoPrazo || null,
      }
      const atualizada = await editarMeta(editandoId, request)
      aoSalvar(atualizada)
      setEditandoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível editar a meta.'))
    } finally {
      setEditandoBtn(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Editar Metas"
      subtitulo="Renomeie, ajuste o valor alvo e o prazo de suas metas."
      aoFechar={aoFechar}
      largura="lg"
    >
      <div className="gerenciar-sub">
        {erro && <p className="erro">{erro}</p>}

        <div className="gerenciar-sub-lista">
          {metas.length === 0 ? (
            <div className="estado-vazio">Nenhuma meta cadastrada.</div>
          ) : (
            metas.map((meta) => (
              <div key={meta.id} className="gerenciar-sub-item">
                {editandoId === meta.id ? (
                  <div className="gerenciar-meta-editar">
                    <input
                      type="text"
                      className="nome"
                      value={editandoNome}
                      onChange={(e) => setEditandoNome(e.target.value)}
                      placeholder="Nome da meta"
                      minLength={2}
                      maxLength={100}
                      autoFocus
                    />
                    <input
                      type="number"
                      className="valor"
                      value={editandoValorAlvo}
                      onChange={(e) => setEditandoValorAlvo(e.target.value)}
                      placeholder="Valor alvo"
                      min="0.01"
                      step="0.01"
                    />
                    <input
                      type="date"
                      className="prazo"
                      value={editandoPrazo}
                      onChange={(e) => setEditandoPrazo(e.target.value)}
                    />
                    <div className="gerenciar-meta-editar-acoes">
                      <button
                        type="button"
                        className="btn btn-primario"
                        onClick={salvarEdicao}
                        disabled={editandoBtn || !editandoNome.trim()}
                      >
                        {editandoBtn ? 'Salvando...' : 'Salvar'}
                      </button>
                      <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={() => setEditandoId(null)}
                      >
                        Cancelar
                      </button>
                    </div>
                  </div>
                ) : (
                  <>
                    <div className="gerenciar-sub-info">
                      <span className="gerenciar-sub-nome">{meta.nome}</span>
                      <span className="gerenciar-sub-conta">
                        {moeda(meta.valorAtual)} de {moeda(meta.valorAlvo)}
                      </span>
                    </div>
                    <div className="gerenciar-sub-acoes">
                      <span className={`meta-prazo ${classePrazo(meta.prazo)}`}>
                        {meta.prazo}
                      </span>
                      <button
                        type="button"
                        className="gerenciar-sub-icone"
                        aria-label="Editar"
                        title="Editar"
                        onClick={() => iniciarEdicao(meta)}
                      >
                        <IconeLapis />
                      </button>
                      <button
                        type="button"
                        className="gerenciar-sub-icone perigo"
                        aria-label="Excluir"
                        title="Excluir"
                        onClick={() => pedirExclusao(meta.id)}
                      >
                        <IconeLixeira />
                      </button>
                    </div>
                  </>
                )}

                {excluindoId === meta.id && (
                  <div className="gerenciar-sub-confirma">
                    <div className="gerenciar-sub-confirma-texto">
                      <span>Excluir "{meta.nome}"?</span>
                    </div>
                    <div className="gerenciar-sub-confirma-acoes">
                      <button
                        type="button"
                        className="btn btn-perigo"
                        onClick={confirmarExclusao}
                        disabled={excluindoBtn}
                      >
                        {excluindoBtn ? 'Excluindo...' : 'Excluir'}
                      </button>
                      <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={() => setExcluindoId(null)}
                      >
                        Cancelar
                      </button>
                    </div>
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </div>
    </Modal>
  )
}

/* ---------- Modal de edição de meta (PUT — edição individual) ---------- */

interface ModalEditarMetaProps {
  meta: Meta | null
  aoFechar: () => void
  aoSalvar: (atualizada: MetaResponse) => void
}

function ModalEditarMeta({ meta, aoFechar, aoSalvar }: ModalEditarMetaProps) {
  const [nome, setNome] = useState('')
  const [valorAlvo, setValorAlvo] = useState('')
  const [prazo, setPrazo] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const aberto = meta !== null

  useEffect(() => {
    if (meta) {
      setNome(meta.nome)
      setValorAlvo(String(meta.valorAlvo))
      setPrazo(meta.prazoData)
      setErro('')
    }
  }, [meta])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!meta) return
    setErro('')
    setCarregando(true)
    try {
      const atualizada = await editarMeta(meta.id, {
        nome: nome.trim(),
        valorAlvo: Number(valorAlvo),
        prazo,
      })
      aoSalvar(atualizada)
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível editar a meta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Editar Meta"
      subtitulo="Atualize o nome, o valor alvo e o prazo da meta."
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Nome da Meta
          <input
            type="text"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            minLength={2}
            maxLength={100}
            required
            autoFocus
          />
        </label>

        <label className="campo">
          Valor Alvo
          <input
            type="number"
            value={valorAlvo}
            onChange={(e) => setValorAlvo(e.target.value)}
            min="0.01"
            step="0.01"
            required
          />
        </label>

        <label className="campo">
          Prazo
          <input
            type="date"
            value={prazo}
            onChange={(e) => setPrazo(e.target.value)}
            required
          />
        </label>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primario" disabled={carregando}>
            {carregando ? 'Salvando...' : 'Salvar Alterações'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

/* ---------- Modal de exclusão de meta (DELETE) ---------- */

interface ModalExcluirMetaProps {
  meta: Meta | null
  aoFechar: () => void
  aoExcluir: (id: number) => void
}

function ModalExcluirMeta({ meta, aoFechar, aoExcluir }: ModalExcluirMetaProps) {
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const aberto = meta !== null

  useEffect(() => {
    if (meta) setErro('')
  }, [meta])

  const handleConfirmar = async () => {
    if (!meta) return
    setErro('')
    setCarregando(true)
    try {
      await excluirMeta(meta.id)
      aoExcluir(meta.id)
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível excluir a meta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Excluir Meta"
      subtitulo="Esta ação não pode ser desfeita."
      aoFechar={aoFechar}
    >
      <div className="formulario">
        <p>
          Tem certeza que deseja excluir a meta <strong>{meta?.nome}</strong>?
        </p>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="button" className="btn btn-perigo" disabled={carregando} onClick={handleConfirmar}>
            {carregando ? 'Excluindo...' : 'Excluir'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

/* ---------- Página principal ---------- */

export default function Metas() {
  const [metas, setMetas] = useState<Meta[]>([])
  const [carregando, setCarregando] = useState(true)
  const [erroBusca, setErroBusca] = useState('')
  const [modalNovaMeta, setModalNovaMeta] = useState(false)
  const [modalGerenciarMetas, setModalGerenciarMetas] = useState(false)
  const [metaEditar, setMetaEditar] = useState<Meta | null>(null)
  const [metaExcluir, setMetaExcluir] = useState<Meta | null>(null)

  const carregarMetas = useCallback(async () => {
    setCarregando(true)
    setErroBusca('')
    try {
      const data = await listarMetas()
      const ativas = data.filter((m) => m.ativo).map(metaParaUi)
      setMetas(ativas)
    } catch (err: unknown) {
      setErroBusca(extrairErro(err, 'Não foi possível carregar as metas.'))
    } finally {
      setCarregando(false)
    }
  }, [])

  useEffect(() => {
    carregarMetas()
  }, [carregarMetas])

  const metaPrincipal = metas.length > 0
    ? metas.reduce((maior, meta) => (progresso(meta) > progresso(maior) ? meta : maior))
    : null
  const totalAcumulado = metas.reduce((acc, meta) => acc + meta.valorAtual, 0)
  const aporte = aporteMensalRecomendado(metas)

  const aoCriar = (nova: Meta) => {
    setMetas((prev) => [nova, ...prev])
  }

  const aoSalvar = (atualizada: MetaResponse) => {
    setMetas((prev) =>
      prev.map((m) => (m.id === atualizada.id ? metaParaUi(atualizada) : m)),
    )
  }

  const aoExcluir = (id: number) => {
    setMetas((prev) => prev.filter((m) => m.id !== id))
  }

  return (
    <div className="pagina">
      <div className="pagina-cabecalho">
        <div>
          <h1 className="pagina-titulo">Metas Financeiras</h1>
          <p className="pagina-subtitulo">Acompanhe seus objetivos e planeje suas conquistas.</p>
        </div>
        <div className="pagina-cabecalho-acoes">
          <button
            type="button"
            className="btn btn-primario"
            onClick={() => setModalNovaMeta(true)}
          >
            <IconeMais />
            <span className="btn-label">Nova Meta</span>
          </button>
          <button
            type="button"
            className="btn btn-primario"
            onClick={() => setModalGerenciarMetas(true)}
          >
            <IconeLapis />
            <span className="btn-label">Metas</span>
          </button>
        </div>
      </div>

      {/* Área 1: Cards de resumo de objetivos */}
      <div className="metas-resumo">
        <ResumoMeta
          rotulo="Meta Principal"
          valor={metaPrincipal ? `${progresso(metaPrincipal)}%` : '—'}
          sub={metaPrincipal ? metaPrincipal.nome : 'Nenhuma meta cadastrada'}
          icone={<IconeAlvo />}
          cor="indigo"
        />
        <ResumoMeta
          rotulo="Total Acumulado em Metas"
          valor={moeda(totalAcumulado)}
          sub={`${metas.length} meta(s) ativa(s)`}
          icone={<IconeCarteira />}
          cor="verde"
        />
        <ResumoMeta
          rotulo="Aporte Mensal Recomendado"
          valor={aporte > 0 ? moeda(aporte) : '—'}
          sub="para manter todas no prazo"
          icone={<IconeSetaCima />}
          cor="ambar"
        />
      </div>

      {/* Área 2: Grade de metas */}
      <h2 className="metas-secao-titulo">Metas Ativas</h2>
      {carregando ? (
        <div className="estado-vazio">Carregando metas...</div>
      ) : erroBusca ? (
        <div className="estado-vazio erro">
          <p>{erroBusca}</p>
          <button type="button" className="btn btn-ghost" onClick={carregarMetas}>
            Tentar novamente
          </button>
        </div>
      ) : metas.length === 0 ? (
        <div className="estado-vazio">Nenhuma meta cadastrada.</div>
      ) : (
        <div className="metas-grade">
          {metas.map((meta) => (
            <MetaCard
              key={meta.id}
              meta={meta}
              onEditar={setMetaEditar}
              onExcluir={setMetaExcluir}
            />
          ))}
        </div>
      )}

      <ModalNovaMeta
        aberto={modalNovaMeta}
        aoFechar={() => setModalNovaMeta(false)}
        aoCriar={aoCriar}
      />

      <ModalGerenciarMetas
        aberto={modalGerenciarMetas}
        aoFechar={() => setModalGerenciarMetas(false)}
        metas={metas}
        aoSalvar={aoSalvar}
        aoExcluir={aoExcluir}
      />

      <ModalEditarMeta
        meta={metaEditar}
        aoFechar={() => setMetaEditar(null)}
        aoSalvar={aoSalvar}
      />

      <ModalExcluirMeta
        meta={metaExcluir}
        aoFechar={() => setMetaExcluir(null)}
        aoExcluir={aoExcluir}
      />
    </div>
  )
}
