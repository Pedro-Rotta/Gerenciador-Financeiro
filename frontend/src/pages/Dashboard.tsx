/**
 * Dashboard — Área de conteúdo principal (Transações, por padrão).
 * 100% visual (dados estáticos/mock), sem chamadas ao backend.
 * Hierarquia:
 *   1. Cabeçalho (saudação + ações)
 *   2. Cards de resumo (saldo, receitas, despesas, meta)
 *   3. Painéis (gráfico de fluxo + resumo por categoria)
 *   4. Tabela de transações recentes
 */

import { useState, useEffect, useCallback, useMemo, type FormEvent, type ReactNode } from 'react'
import { useOutletContext } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import type { Secao } from '../components/Sidebar'
import Modal from '../components/Modal'
import OpcoesDeConta from '../components/OpcoesDeConta'
import FiltrosTransacoes from '../components/FiltrosTransacoes'
import SeletorPeriodo from '../components/SeletorPeriodo'
import {
  chaveDaConta,
  descricaoPeriodo,
  filtrarTransacoes,
  intervaloDoPeriodo,
  PERIODOS,
  type FiltroPeriodo,
  type Filtros,
  type OpcaoCategoria,
  type Periodo,
} from '../utils/filtrosTransacoes'
import {
  alternarCriterio,
  ordenarTransacoes,
  paraParametroOrdenacao,
  SENTIDO_PADRAO,
  type ColunaOrdenacao,
  type CriterioOrdenacao,
} from '../utils/ordenacaoTransacoes'
import { definirSaldoInicial, listarContas } from '../services/contas'
import type { ContaResponse } from '../types/conta'
import { listarContasConjuntas } from '../services/contasConjuntas'
import {
  carregarOpcoesConta,
  filtroDaConta,
  montarOpcoesConta,
  pertenceAConta,
  type OpcaoConta,
} from '../services/opcoesConta'
import { listarCategoriasPorConta, listarCategoriasPorContaConjunta } from '../services/categorias'
import {
  listarTransacoes,
  criarTransacao,
  editarTransacao,
  excluirTransacao,
  pararRecorrencia,
  baixarPdfTransacoes,
} from '../services/transacoes'
import { baixarArquivo } from '../utils/download'
import { preferenciasUso } from '../utils/preferenciasUso'
import { executarVerificacao } from '../services/verificacao'
import { listarOcorrenciasPrevistas } from '../services/recorrencias'
import type { OcorrenciaPrevistaResponse } from '../types/recorrencia'
import { listarMetas } from '../services/metas'
import type { CategoriaResponse } from '../types/categoria'
import { TipoCategoria } from '../types/categoria'
import type { SubCategoriaResponse } from '../types/subcategoria'
import type {
  TransacaoResponse,
  CriarTransacaoRequest,
  EditarTransacaoRequest,
} from '../types/transacao'
import {
  TipoTransacao,
  ClassificacaoTransacao,
  StatusTransacao,
  PeriodicidadeTransacao,
  EscopoExclusao,
} from '../types/transacao'
import { dataLocalISO } from '../utils/data'
import { extrairErro } from '../utils/erros'
import {
  IconeCarteira,
  IconeSetaCima,
  IconeSetaBaixo,
  IconeMeta,
  IconeTransacoes,
  IconeDownload,
  IconeContas,
  IconeCategorias,
  IconeRelatorios,
  IconeCartaoEntrada,
  IconeCartaoSaida,
  IconeLapis,
  IconeLixeira,
  IconeRepetirParar,
} from '../components/icons'

/* ---------- Tipos e dados (mock) ---------- */

interface Resumo {
  rotulo: string
  valor: string
  tendencia: string
  icone: 'indigo' | 'verde' | 'vermelho' | 'ambar'
  tendenciaTipo: 'alta' | 'baixa'
}

interface CategoriaGasto {
  nome: string
  valor: string
  pct: number
  cor: string
}

interface Pendencia {
  /** Chave única na lista: "t:ID" (transação) ou "p:SÉRIE:DATA" (prevista) */
  chave: string
  /** Transação lançada; null nas previstas */
  transacaoId: number | null
  /** Ocorrência mensal prevista (ainda não lançada, fora do saldo) */
  prevista: { recorrenciaId: number; data: string; rotulo: string } | null
  /** "yyyy-MM-dd", para ordenar */
  dataIso: string
  nome: string
  categoria: string
  conta: string
  data: string
  valor: number
  status: StatusTransacao
  tipo: 'entrada' | 'saida'
  /** Só o autor, e só a partir do dia da conta (não se paga o que ainda não chegou) */
  podeConfirmar: boolean
}

interface Transacao {
  id: number
  nome: string
  categoria: string
  conta: string
  data: string
  valor: number
  tipo: 'entrada' | 'saida'
  icone: 'entrada' | 'saida'
  recorrencia: EtiquetaRecorrencia | null
}

/** Filtros iniciais da tabela de transações recentes */
const FILTROS_PADRAO: Filtros = {
  tipo: 'todas',
  periodo: 'semana',
  de: '',
  ate: '',
  conta: '',
  categoria: '',
}

/* ---------- Helpers ---------- */

function moeda(valor: number, tipo: 'entrada' | 'saida') {
  const texto = valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  return { texto, classe: tipo === 'entrada' ? 'valor-entrada' : 'valor-saida' }
}

function moedaSimples(valor: number) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-')
  if (!ano || !mes || !dia) return data
  return `${dia}/${mes}/${ano}`
}

interface EtiquetaRecorrencia {
  texto: string
  encerrada: boolean
}

/** Etiqueta das ocorrências de série: "Mensal", "Semanal · 3 de 10"; null para lançamento avulso */
function etiquetaRecorrencia(t: TransacaoResponse): EtiquetaRecorrencia | null {
  if (t.recorrenciaId == null) return null
  const periodicidade = t.periodicidade === PeriodicidadeTransacao.SEMANAL ? 'Semanal' : 'Mensal'
  const parcela =
    t.totalOcorrencias != null && t.numeroOcorrencia != null
      ? ` · ${t.numeroOcorrencia} de ${t.totalOcorrencias}`
      : ''
  return { texto: periodicidade + parcela, encerrada: t.serieAtiva === false }
}

/** Tags da série: periodicidade (e parcela) e, se a série parou, "Encerrada" */
function TagsRecorrencia({ etiqueta }: { etiqueta: EtiquetaRecorrencia | null }) {
  if (!etiqueta) return null
  return (
    <>
      <span className="tag azul">{etiqueta.texto}</span>
      {etiqueta.encerrada && <span className="tag fosco">Encerrada</span>}
    </>
  )
}

const CORES_CATEGORIA = [
  'var(--primario)',
  'var(--violeta)',
  'var(--info)',
  'var(--alerta)',
  'var(--sucesso)',
]

/**
 * Soma as despesas do período por categoria. Agrupa pelo nome: a mesma categoria
 * em contas diferentes (ex.: "Comida" na conta e na conjunta) vira uma linha só.
 * Ordena da maior para a menor despesa.
 */
function calcularGastosPorCategoria(
  transacoes: TransacaoResponse[],
  categorias: CategoriaResponse[],
  periodo: Periodo,
): CategoriaGasto[] {
  const catPorId = new Map(categorias.map((c) => [c.id, c]))
  const { de, ate } = intervaloDoPeriodo(periodo)
  // Só as pagas, como o saldo e os relatórios: pendentes e atrasadas entram quando forem pagas
  const despesas = transacoes.filter(
    (t) =>
      t.tipo === TipoTransacao.SAIDA &&
      t.status === StatusTransacao.CONCLUIDA &&
      (!de || t.dataTransacao >= de) &&
      (!ate || t.dataTransacao <= ate),
  )
  const total = despesas.reduce((acc, t) => acc + t.valor, 0)
  if (total <= 0) return []
  const porNome = new Map<string, number>()
  despesas.forEach((t) => {
    const nome = t.categoriaId != null ? (catPorId.get(t.categoriaId)?.nome ?? 'Sem categoria') : 'Sem categoria'
    porNome.set(nome, (porNome.get(nome) ?? 0) + t.valor)
  })
  return Array.from(porNome.entries())
    .sort((a, b) => b[1] - a[1])
    .map(([nome, valor], i) => ({
      nome,
      valor: moedaSimples(valor),
      pct: Math.round((valor / total) * 100),
      cor: CORES_CATEGORIA[i % CORES_CATEGORIA.length],
    }))
}

/** Opções do filtro de período do painel "Gastos por categoria" */
const PERIODOS_GASTOS: { valor: FiltroPeriodo; rotulo: string }[] = [
  { valor: 'semana', rotulo: 'Semana' },
  { valor: 'mes', rotulo: 'Mês' },
  { valor: '3-meses', rotulo: 'Tri' },
  { valor: '6-meses', rotulo: 'Semestre' },
  { valor: 'personalizado', rotulo: 'Personalizado' },
]

function mapearTransacoes(
  transacoes: TransacaoResponse[],
  categorias: CategoriaResponse[],
  nomeConta: (t: TransacaoResponse) => string,
): Transacao[] {
  const catPorId = new Map(categorias.map((c) => [c.id, c]))
  return transacoes.map((t) => {
    const categoria = t.categoriaId != null ? catPorId.get(t.categoriaId) : undefined
    const tipo = t.tipo === TipoTransacao.ENTRADA ? 'entrada' : 'saida'
    return {
      id: t.id,
      nome: t.descricao || 'Sem descrição',
      categoria: categoria?.nome ?? 'Sem categoria',
      conta: nomeConta(t),
      data: formatarData(t.dataTransacao),
      valor: t.valor,
      tipo,
      icone: tipo as 'entrada' | 'saida',
      recorrencia: etiquetaRecorrencia(t),
    }
  })
}

type NomeDaConta = (t: { contaId: number | null; contaConjuntaId: number | null }) => string

function mapearPendencias(
  transacoes: TransacaoResponse[],
  categorias: CategoriaResponse[],
  nomeConta: NomeDaConta,
  membroId: number | null,
): Pendencia[] {
  const catPorId = new Map(categorias.map((c) => [c.id, c]))
  return transacoes
    .filter(
      (t) => t.status === StatusTransacao.PENDENTE || t.status === StatusTransacao.ATRASADA,
    )
    .map((t) => {
      const categoria = t.categoriaId != null ? catPorId.get(t.categoriaId) : undefined
      return {
        chave: `t:${t.id}`,
        transacaoId: t.id,
        prevista: null,
        dataIso: t.dataTransacao,
        nome: t.descricao || 'Sem descrição',
        categoria: categoria?.nome ?? 'Sem categoria',
        conta: nomeConta(t),
        data: formatarData(t.dataTransacao),
        valor: t.valor,
        status: t.status,
        tipo: t.tipo === TipoTransacao.ENTRADA ? 'entrada' : 'saida',
        podeConfirmar: t.membroId === membroId && t.dataTransacao <= dataLocalISO(),
      }
    })
}

/** Mensais do mês atual e do próximo ainda não lançadas: "Prevista · 6 de 10" ou "Prevista · Mensal" */
function mapearPrevistas(
  previstas: OcorrenciaPrevistaResponse[],
  categorias: CategoriaResponse[],
  nomeConta: NomeDaConta,
): Pendencia[] {
  const catPorId = new Map(categorias.map((c) => [c.id, c]))
  return previstas.map((o) => ({
    chave: `p:${o.recorrenciaId}:${o.dataPrevista}`,
    transacaoId: null,
    prevista: {
      recorrenciaId: o.recorrenciaId,
      data: o.dataPrevista,
      rotulo: o.totalOcorrencias != null ? `${o.numeroOcorrencia} de ${o.totalOcorrencias}` : 'Mensal',
    },
    dataIso: o.dataPrevista,
    nome: o.descricao || 'Sem descrição',
    categoria: (o.categoriaId != null ? catPorId.get(o.categoriaId)?.nome : undefined) ?? 'Sem categoria',
    conta: nomeConta(o),
    data: formatarData(o.dataPrevista),
    valor: o.valor,
    status: StatusTransacao.PENDENTE,
    tipo: o.tipo === TipoTransacao.ENTRADA ? 'entrada' : 'saida',
    // Prevista é sempre futura: só pode ser paga quando for lançada e chegar o dia
    podeConfirmar: false,
  }))
}

/* ---------- Sub-componentes ---------- */

// Colunas da tabela que ordenam pela seta; aria: sentido padrão de cada uma
const COLUNAS_ORDENAVEIS: { coluna: ColunaOrdenacao; rotulo: string; padraoCrescente: boolean; direita?: boolean }[] = [
  { coluna: 'descricao', rotulo: 'Descrição', padraoCrescente: true },
  { coluna: 'categoria', rotulo: 'Categoria', padraoCrescente: true },
  { coluna: 'conta', rotulo: 'Conta', padraoCrescente: true },
  { coluna: 'data', rotulo: 'Data', padraoCrescente: false },
  { coluna: 'valor', rotulo: 'Valor', padraoCrescente: false, direita: true },
]

function TabelaTransacoes({
  transacoes,
  sub,
  filtros,
  ordenacao,
  aoOrdenar,
}: {
  transacoes: Transacao[]
  sub: string
  filtros: ReactNode
  ordenacao: CriterioOrdenacao[]
  aoOrdenar: (coluna: ColunaOrdenacao) => void
}) {
  return (
    <DashboardSection titulo="Transações recentes" sub={sub} acoes={filtros}>
      <div className="tabela-wrapper">
        <table className="tabela">
          <thead>
            <tr>
              {COLUNAS_ORDENAVEIS.map(({ coluna, rotulo, padraoCrescente, direita }) => {
                const criterio = ordenacao.find((c) => c.coluna === coluna)
                const crescente = criterio ? padraoCrescente !== criterio.invertido : undefined
                const proximo = !criterio
                  ? `ordenar: ${SENTIDO_PADRAO[coluna]}`
                  : criterio.invertido
                    ? 'tirar da ordenação'
                    : 'inverter a ordem'
                return (
                  <th
                    key={coluna}
                    style={direita ? { textAlign: 'right' } : undefined}
                    aria-sort={crescente === undefined ? 'none' : crescente ? 'ascending' : 'descending'}
                  >
                    <button
                      type="button"
                      className={`tabela-ordenar ${criterio ? 'ativa' : ''}`}
                      onClick={() => aoOrdenar(coluna)}
                      title={`${rotulo}: ${proximo}`}
                    >
                      {rotulo}
                      <span className="tabela-ordenar-seta" aria-hidden="true">
                        {!criterio ? '↕' : criterio.invertido ? '↑' : '↓'}
                      </span>
                    </button>
                  </th>
                )
              })}
            </tr>
          </thead>
          <tbody>
            {transacoes.length === 0 ? (
              <tr>
                <td colSpan={5} className="estado-vazio" style={{ textAlign: 'center' }}>
                  Nenhuma transação encontrada para os filtros escolhidos.
                </td>
              </tr>
            ) : (
              transacoes.map((t) => {
                const { texto, classe } = moeda(t.valor, t.tipo)
                return (
                  <tr key={t.id}>
                    <td>
                      <div className="td-celula">
                        <span className={`td-avatar ${t.icone}`}>
                          {t.icone === 'entrada' ? <IconeCartaoEntrada /> : <IconeCartaoSaida />}
                        </span>
                        <div>
                          <div className="td-nome">{t.nome}</div>
                          {t.recorrencia && (
                            <div className="td-tags">
                              <TagsRecorrencia etiqueta={t.recorrencia} />
                            </div>
                          )}
                        </div>
                      </div>
                    </td>
                    <td>
                      <span className={`tag ${t.tipo === 'entrada' ? 'verde' : 'vermelho'}`}>
                        {t.categoria}
                      </span>
                    </td>
                    <td>{t.conta}</td>
                    <td>{t.data}</td>
                    <td className={classe} style={{ textAlign: 'right' }}>
                      {texto}
                    </td>
                  </tr>
                )
              })
            )}
          </tbody>
        </table>
      </div>
    </DashboardSection>
  )
}

function ResumoCategorias({
  categorias,
  periodo,
  aoMudarPeriodo,
}: {
  categorias: CategoriaGasto[]
  periodo: Periodo
  aoMudarPeriodo: (periodo: Periodo) => void
}) {
  return (
    <DashboardSection
      titulo="Gastos por categoria"
      sub={`Distribuição das despesas pagas ${descricaoPeriodo(periodo.periodo)}`}
      acoes={<SeletorPeriodo valor={periodo} aoMudar={aoMudarPeriodo} opcoes={PERIODOS_GASTOS} />}
    >
      {categorias.length === 0 ? (
        <div className="painel-sub">Nenhuma despesa no período.</div>
      ) : (
        categorias.map((categoria) => (
          <div key={categoria.nome} style={{ marginBottom: '1rem' }}>
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                fontSize: '0.85rem',
                fontWeight: 600,
                marginBottom: '0.35rem',
              }}
            >
              <span>{categoria.nome}</span>
              <span>{categoria.valor}</span>
            </div>
            <div
              style={{
                height: '8px',
                background: 'var(--bg-suave)',
                borderRadius: 999,
                overflow: 'hidden',
              }}
            >
              <div
                style={{
                  width: `${categoria.pct}%`,
                  height: '100%',
                  background: categoria.cor,
                  borderRadius: 999,
                  transition: 'width 0.5s ease',
                }}
              />
            </div>
          </div>
        ))
      )}
    </DashboardSection>
  )
}

function PainelPendencias({
  itens,
  aoConfirmarPagamento,
}: {
  itens: Pendencia[]
  /** Marca a pendência como CONCLUÍDA (conta paga / valor recebido) */
  aoConfirmarPagamento: (item: Pendencia) => Promise<void>
}) {
  // Pendência com a confirmação "A conta foi paga?" aberta (pela chave)
  const [confirmandoId, setConfirmandoId] = useState<string | null>(null)
  const [salvando, setSalvando] = useState(false)
  const [erro, setErro] = useState('')

  const confirmar = async (item: Pendencia) => {
    setErro('')
    setSalvando(true)
    try {
      await aoConfirmarPagamento(item)
      setConfirmandoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível marcar como concluída.'))
    } finally {
      setSalvando(false)
    }
  }

  return (
    <DashboardSection titulo="Pendências de pagamento" sub="Transações a receber/pagar">
      {erro && <p className="erro">{erro}</p>}
      {itens.length === 0 ? (
        <div className="painel-sub">Nenhuma pendência.</div>
      ) : (
        <div className="pendencias-lista">
          {itens.map((item) => (
            <div className="pendencia-item" key={item.chave}>
              <div className="pendencia-item-info">
                <div className="pendencia-item-nome">{item.nome}</div>
                <div className="pendencia-item-sub">
                  {item.categoria} • {item.conta} • {item.data}
                </div>
                {item.prevista && (
                  <div className="td-tags">
                    <span className="tag azul">Prevista · {item.prevista.rotulo}</span>
                  </div>
                )}
              </div>
              <div className="pendencia-item-direita">
                <div
                  className={`pendencia-item-valor ${
                    item.status === StatusTransacao.ATRASADA ? 'atrasada' : ''
                  }`}
                >
                  {moedaSimples(item.valor)}
                </div>
                <span
                  className={`tag ${
                    item.status === StatusTransacao.ATRASADA ? 'vermelho' : 'ambar'
                  }`}
                >
                  {item.status === StatusTransacao.ATRASADA ? 'Atrasada' : 'Pendente'}
                </span>
                {item.podeConfirmar && confirmandoId !== item.chave && (
                  <button
                    type="button"
                    className="pendencia-pagar"
                    onClick={() => {
                      setErro('')
                      setConfirmandoId(item.chave)
                    }}
                  >
                    {item.tipo === 'entrada' ? 'Recebido' : 'Conta paga'}
                  </button>
                )}
              </div>

              {confirmandoId === item.chave && (
                <div className="pendencia-confirma">
                  <span>
                    {item.tipo === 'entrada' ? 'O valor foi recebido?' : 'A conta foi paga?'}
                  </span>
                  <div className="pendencia-confirma-acoes">
                    <button
                      type="button"
                      className="btn btn-primario"
                      onClick={() => confirmar(item)}
                      disabled={salvando}
                    >
                      {salvando ? 'Salvando...' : item.tipo === 'entrada' ? 'Sim, recebi' : 'Sim, foi paga'}
                    </button>
                    <button
                      type="button"
                      className="btn btn-ghost"
                      onClick={() => setConfirmandoId(null)}
                      disabled={salvando}
                    >
                      Cancelar
                    </button>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </DashboardSection>
  )
}

/* Painel genérico reutilizável (acoes: controles à direita do título, ex.: filtros) */
function DashboardSection({
  titulo,
  sub,
  acoes,
  children,
}: {
  titulo: string
  sub: string
  acoes?: ReactNode
  children: ReactNode
}) {
  return (
    <div className="painel">
      <div className={`painel-cabecalho ${acoes ? 'com-acoes' : ''}`}>
        <div>
          <div className="painel-titulo">{titulo}</div>
          <div className="painel-sub">{sub}</div>
        </div>
        {acoes}
      </div>
      {children}
    </div>
  )
}

/* ---------- Página principal ---------- */

export default function Dashboard() {
  // A seção ativa vem do Layout (via Outlet context) — apenas visual.
  const { secaoAtiva } = useOutletContext<{ secaoAtiva: Secao }>()
  const { nome, membroId } = useAuth()
  const [modalNovaTransacao, setModalNovaTransacao] = useState(false)
  const [modalGerenciarTransacoes, setModalGerenciarTransacoes] = useState(false)

  // Dados brutos de todas as contas: a tabela aplica os filtros sobre eles
  const [transacoesBrutas, setTransacoesBrutas] = useState<TransacaoResponse[]>([])
  const [categoriasTodas, setCategoriasTodas] = useState<CategoriaResponse[]>([])
  const [opcoesConta, setOpcoesConta] = useState<OpcaoConta[]>([])
  const [filtros, setFiltros] = useState<Filtros>(FILTROS_PADRAO)
  // Colunas marcadas nas setas da tabela, na ordem em que foram marcadas (vale também para o PDF)
  const [ordenacao, setOrdenacao] = useState<CriterioOrdenacao[]>([])
  const [erroTransacoes, setErroTransacoes] = useState('')
  const [resumo, setResumo] = useState<Resumo[]>([])
  // Preferência "Ocultar valores ao abrir": os cards mostram •••• até o membro tocar
  const [valoresOcultos, setValoresOcultos] = useState(() => preferenciasUso().ocultarValores)
  const [periodoGastos, setPeriodoGastos] = useState<Periodo>({ periodo: 'mes', de: '', ate: '' })
  const [pendencias, setPendencias] = useState<Pendencia[]>([])
  // Contas individuais que ainda não receberam o saldo inicial (não aceitam transações)
  const [contasSemSaldoInicial, setContasSemSaldoInicial] = useState<ContaResponse[]>([])
  const [contaSaldoInicial, setContaSaldoInicial] = useState<ContaResponse | null>(null)

  const carregarTransacoes = useCallback(async () => {
    setErroTransacoes('')
    try {
      const [contas, contasConjuntas] = await Promise.all([listarContas(), listarContasConjuntas()])
      const saldoTotal = contas.reduce((acc, c) => acc + c.saldo, 0)
      // Previsto: saldo + pendentes/atrasadas (a listagem já soma por conta)
      const previstoTotal = contas.reduce((acc, c) => acc + (c.saldoPrevisto ?? c.saldo), 0)
      setContasSemSaldoInicial(contas.filter((c) => !c.saldoInicialDefinido))

      const contaPorId = new Map(contas.map((c) => [c.id, c.nome]))
      const contaConjuntaPorId = new Map(contasConjuntas.map((c) => [c.id, c.nome]))
      const nomeConta: NomeDaConta = (t) =>
        t.contaId != null
          ? (contaPorId.get(t.contaId) ?? '—')
          : t.contaConjuntaId != null
            ? (contaConjuntaPorId.get(t.contaConjuntaId) ?? '—')
            : '—'

      // Transações e categorias de todas as contas: individuais e conjuntas
      const resultados = await Promise.all([
        ...contas.map(async (conta) => {
          const [t, c] = await Promise.all([
            listarTransacoes({ contaId: conta.id }),
            listarCategoriasPorConta(conta.id),
          ])
          return { transacoes: t, categorias: c }
        }),
        ...contasConjuntas.map(async (conta) => {
          const [t, c] = await Promise.all([
            listarTransacoes({ contaConjuntaId: conta.id }),
            listarCategoriasPorContaConjunta(conta.id),
          ])
          return { transacoes: t, categorias: c }
        }),
      ])
      let transacoesData: TransacaoResponse[] = []
      let categorias: CategoriaResponse[] = []
      resultados.forEach((r) => {
        transacoesData = transacoesData.concat(r.transacoes)
        categorias = categorias.concat(r.categorias)
      })
      setTransacoesBrutas(transacoesData)
      setCategoriasTodas(categorias)
      setOpcoesConta(montarOpcoesConta(contas, contasConjuntas))
      // Pendências lançadas + previstas das mensais (mês atual e próximo), por data
      let previstas: OcorrenciaPrevistaResponse[] = []
      try {
        previstas = await listarOcorrenciasPrevistas()
      } catch {
        // sem a prévia, mostra só as lançadas
      }
      setPendencias(
        [
          ...mapearPendencias(transacoesData, categorias, nomeConta, membroId),
          ...mapearPrevistas(previstas, categorias, nomeConta),
        ].sort((a, b) => a.dataIso.localeCompare(b.dataIso)),
      )

      const hoje = dataLocalISO()
      // Recebido/pago hoje: só as concluídas, como o saldo
      const transacoesDoDia = transacoesData.filter(
        (t) => t.dataTransacao === hoje && t.status === StatusTransacao.CONCLUIDA,
      )

      const receitas = transacoesDoDia
        .filter((t) => t.tipo === TipoTransacao.ENTRADA)
        .reduce((acc, t) => acc + t.valor, 0)
      const despesas = transacoesDoDia
        .filter((t) => t.tipo === TipoTransacao.SAIDA)
        .reduce((acc, t) => acc + t.valor, 0)

      let metaResumo: Resumo = {
        rotulo: 'Meta de economia',
        valor: '—',
        tendencia: 'Nenhuma meta cadastrada',
        icone: 'ambar',
        tendenciaTipo: 'alta',
      }
      try {
        const metas = await listarMetas()
        const meta = metas.find((m) => m.ativo)
        if (meta) {
          const pct = meta.valorAlvo > 0 ? Math.round((meta.valorAtual / meta.valorAlvo) * 100) : 0
          metaResumo = {
            rotulo: 'Meta de economia',
            valor: `${pct}%`,
            tendencia:
              `${moedaSimples(meta.valorAtual)} de ${moedaSimples(meta.valorAlvo)}` +
              (meta.valorAtual > meta.valorAlvo
                ? ` · excedeu ${moedaSimples(meta.valorAtual - meta.valorAlvo)}`
                : ''),
            icone: 'ambar',
            tendenciaTipo: pct >= 100 ? 'alta' : 'baixa',
          }
        }
      } catch {
        // mantém o fallback caso a listagem de metas falhe
      }

      setResumo([
        {
          rotulo: 'Saldo total',
          valor: moedaSimples(saldoTotal),
          tendencia: `Previsto: ${moedaSimples(previstoTotal)} · ${contas.length} conta(s)`,
          icone: 'indigo',
          tendenciaTipo: previstoTotal >= saldoTotal ? 'alta' : 'baixa',
        },
        {
          rotulo: 'Receitas',
          valor: moedaSimples(receitas),
          tendencia: 'Recebidas hoje',
          icone: 'verde',
          tendenciaTipo: 'alta',
        },
        {
          rotulo: 'Despesas',
          valor: moedaSimples(despesas),
          tendencia: 'Pagas hoje',
          icone: 'vermelho',
          tendenciaTipo: 'baixa',
        },
        metaResumo,
      ])
    } catch (err: unknown) {
      setErroTransacoes(extrairErro(err, 'Não foi possível carregar as transações.'))
    }
  }, [membroId])

  // Espera a verificação (gera ocorrências, marca atrasadas) para já mostrar o status certo
  useEffect(() => {
    executarVerificacao()
      .catch(() => {
        // a verificação é opcional: o painel carrega mesmo se ela falhar
      })
      .finally(() => carregarTransacoes())
  }, [carregarTransacoes])

  // Recarrega os dados após criar/editar/excluir uma transação.
  const aoAlterar = useCallback(() => {
    carregarTransacoes()
  }, [carregarTransacoes])

  // "Conta paga" / "Recebido" nas pendências: a transação vira CONCLUÍDA e os painéis recarregam
  const confirmarPagamento = useCallback(
    async (item: Pendencia) => {
      if (item.transacaoId == null) return
      await editarTransacao(item.transacaoId, { status: StatusTransacao.CONCLUIDA })
      await carregarTransacoes()
    },
    [carregarTransacoes],
  )

  // Painel "Gastos por categoria": despesas do período escolhido
  const categoriasGastos = useMemo(
    () => calcularGastosPorCategoria(transacoesBrutas, categoriasTodas, periodoGastos),
    [transacoesBrutas, categoriasTodas, periodoGastos],
  )

  // Tabela: aplica os filtros e ordena da mais recente para a mais antiga
  const transacoesFiltradas = useMemo(() => {
    const nomeCategoriaPorId = new Map(categoriasTodas.map((c) => [c.id, c.nome]))
    const nomeContaPorChave = new Map(opcoesConta.map((c) => [c.chave, c.nome]))
    const nomeCategoria = (t: TransacaoResponse) =>
      t.categoriaId != null ? (nomeCategoriaPorId.get(t.categoriaId) ?? null) : null
    // Posição da conta na ordem dos seletores: principal, individuais A-Z, conjuntas A-Z
    const posicaoPorChave = new Map(opcoesConta.map((c, i) => [c.chave, i]))
    // Filtra antes de ordenar: ordena só o que aparece
    const filtradas = ordenarTransacoes(filtrarTransacoes(transacoesBrutas, filtros, nomeCategoria), ordenacao, {
      nomeCategoria,
      posicaoConta: (t) => posicaoPorChave.get(chaveDaConta(t)) ?? Number.MAX_SAFE_INTEGER,
    })
    return mapearTransacoes(filtradas, categoriasTodas, (t) => nomeContaPorChave.get(chaveDaConta(t)) ?? '—')
  }, [transacoesBrutas, categoriasTodas, opcoesConta, filtros, ordenacao])

  // Categorias do seletor: as da conta filtrada (ou de todas), sem repetir nome
  const opcoesCategoria = useMemo<OpcaoCategoria[]>(() => {
    const conta = opcoesConta.find((c) => c.chave === filtros.conta)
    const vistas = new Set<string>()
    return categoriasTodas
      .filter((c) => !conta || pertenceAConta(c, conta))
      .map((c): OpcaoCategoria => ({ nome: c.nome, tipo: c.tipo === TipoCategoria.ENTRADA ? 'entrada' : 'saida' }))
      .filter((c) => {
        const chave = `${c.tipo}:${c.nome}`
        if (vistas.has(chave)) return false
        vistas.add(chave)
        return true
      })
  }, [categoriasTodas, opcoesConta, filtros.conta])

  const quantidade = transacoesFiltradas.length
  const subTabela =
    `Últimas movimentações ${descricaoPeriodo(filtros.periodo)} · ` +
    `${quantidade} ${quantidade === 1 ? 'transação' : 'transações'}`

  // Exportar PDF: o backend gera o PDF das transações com os filtros da tabela
  const [gerandoPdf, setGerandoPdf] = useState(false)
  const [erroExportacao, setErroExportacao] = useState('')
  const exportarPdf = async () => {
    const { de, ate } = intervaloDoPeriodo(filtros)
    const conta = opcoesConta.find((c) => c.chave === filtros.conta)
    setErroExportacao('')
    setGerandoPdf(true)
    try {
      const { arquivo, nomeArquivo } = await baixarPdfTransacoes({
        tipo:
          filtros.tipo === 'entradas'
            ? TipoTransacao.ENTRADA
            : filtros.tipo === 'saidas'
              ? TipoTransacao.SAIDA
              : undefined,
        dataInicio: de ?? undefined,
        dataFim: ate ?? undefined,
        contaId: conta && !conta.conjunta ? conta.id : undefined,
        contaConjuntaId: conta?.conjunta ? conta.id : undefined,
        categoria: filtros.categoria || undefined,
        periodo: PERIODOS.find((p) => p.valor === filtros.periodo)?.rotulo,
        // O PDF sai na mesma ordem da tabela
        ordenacao: paraParametroOrdenacao(ordenacao),
      })
      baixarArquivo(arquivo, nomeArquivo)
    } catch {
      setErroExportacao('Não foi possível gerar o PDF. Tente novamente.')
    } finally {
      setGerandoPdf(false)
    }
  }

  // Como a página foca na área de Transações, filtramos para manter o foco no dashboard.
  void secaoAtiva

  return (
    <div className="pagina">
      <div className="pagina-cabecalho">
        <div>
          <h1 className="pagina-titulo">Olá {nome ?? 'visitante'}, seja bem-vindo 👋</h1>
          <p className="pagina-subtitulo">
            Aqui está o resumo das suas finanças hoje.
          </p>
        </div>
        <div className="pagina-cabecalho-acoes">
          <button
            type="button"
            className="btn btn-ghost"
            onClick={exportarPdf}
            disabled={gerandoPdf}
            title="Baixa em PDF as transações com os filtros da tabela abaixo"
          >
            <IconeDownload />
            <span className="btn-label">{gerandoPdf ? 'Gerando...' : 'Exportar PDF'}</span>
          </button>
          {/* Conta individual nova: o saldo inicial vem antes da primeira transação */}
          {contasSemSaldoInicial.map((conta) => (
            <button
              key={conta.id}
              type="button"
              className="btn btn-primario btn-com-rotulo"
              onClick={() => setContaSaldoInicial(conta)}
              title="Informe o saldo da conta antes das transações para começar a lançá-las"
            >
              <IconeCarteira />
              <span className="btn-label">Saldo inicial · {conta.nome}</span>
            </button>
          ))}
          <button
            type="button"
            className="btn btn-primario"
            onClick={() => setModalNovaTransacao(true)}
            disabled={!opcoesConta.some((o) => o.liberada)}
            title={
              opcoesConta.some((o) => o.liberada)
                ? undefined
                : 'Informe o saldo inicial de uma conta para lançar transações'
            }
          >
            <IconeTransacoes />
            <span className="btn-label">Nova transação</span>
          </button>
          <button
            type="button"
            className="btn btn-primario"
            onClick={() => setModalGerenciarTransacoes(true)}
          >
            <IconeLapis />
            <span className="btn-label">Transações</span>
          </button>
        </div>
      </div>
      {erroExportacao && <p className="erro">{erroExportacao}</p>}

      {/* Visível também no celular, onde os botões ficam só com ícone */}
      {contasSemSaldoInicial.length > 0 && (
        <p className="aviso-saldo-inicial">
          {contasSemSaldoInicial.length === 1
            ? `A conta "${contasSemSaldoInicial[0].nome}" ainda não tem saldo inicial: informe-o em "Saldo inicial" para lançar transações nela.`
            : `As contas ${contasSemSaldoInicial.map((c) => `"${c.nome}"`).join(', ')} ainda não têm saldo inicial: informe-o em "Saldo inicial" para lançar transações nelas.`}
        </p>
      )}

      {/* Cards de resumo */}
      <section className="cards-resumo" aria-label="Resumo financeiro">
        {resumo.map((card) => (
          <div className="card-resumo" key={card.rotulo}>
            <span className={`card-resumo-icone ${card.icone}`}>
              {card.icone === 'indigo' && <IconeCarteira />}
              {card.icone === 'verde' && <IconeSetaCima />}
              {card.icone === 'vermelho' && <IconeSetaBaixo />}
              {card.icone === 'ambar' && <IconeMeta />}
            </span>
            <div className="card-resumo-info">
              <div className="card-resumo-rotulo">{card.rotulo}</div>
              <div className="card-resumo-valor">
                {valoresOcultos ? (
                  <button
                    type="button"
                    className="valor-oculto"
                    aria-label="Mostrar os valores"
                    onClick={() => setValoresOcultos(false)}
                  >
                    ••••
                  </button>
                ) : (
                  card.valor
                )}
              </div>
              <div className={`card-resumo-tendencia ${card.tendenciaTipo}`}>
                {card.tendenciaTipo === 'alta' ? <IconeSetaCima /> : <IconeSetaBaixo />}
                {valoresOcultos ? 'Toque em •••• para ver' : card.tendencia}
              </div>
            </div>
          </div>
        ))}
      </section>

      {/* Painéis lado a lado */}
      <div className="paineis">
        <PainelPendencias itens={pendencias} aoConfirmarPagamento={confirmarPagamento} />
        <ResumoCategorias
          categorias={categoriasGastos}
          periodo={periodoGastos}
          aoMudarPeriodo={setPeriodoGastos}
        />
      </div>

      {/* Tabela de transações */}
      <TabelaTransacoes
        transacoes={transacoesFiltradas}
        sub={subTabela}
        ordenacao={ordenacao}
        aoOrdenar={(coluna) => setOrdenacao((atual) => alternarCriterio(atual, coluna))}
        filtros={
          <FiltrosTransacoes
            valor={filtros}
            aoMudar={setFiltros}
            padrao={FILTROS_PADRAO}
            contas={opcoesConta}
            categorias={opcoesCategoria}
          />
        }
      />

      {erroTransacoes && (
        <div className="painel">
          <p className="erro">{erroTransacoes}</p>
        </div>
      )}

      <ModalNovaTransacao
        aberto={modalNovaTransacao}
        aoFechar={() => setModalNovaTransacao(false)}
        aoCriada={aoAlterar}
      />

      <ModalSaldoInicial
        conta={contaSaldoInicial}
        aoFechar={() => setContaSaldoInicial(null)}
        aoSalvar={aoAlterar}
      />

      <ModalGerenciarTransacoes
        aberto={modalGerenciarTransacoes}
        aoFechar={() => setModalGerenciarTransacoes(false)}
        aoAlterada={aoAlterar}
      />

      <BottomNavHint />
    </div>
  )
}

/* Aviso decorativo: as demais seções ainda não foram construídas */
function BottomNavHint() {
  const secaoMap = {
    transacoes: IconeTransacoes,
    contas: IconeContas,
    contasConjuntas: IconeContas,
    categorias: IconeCategorias,
    metas: IconeMeta,
    relatorios: IconeRelatorios,
  } as const
  const { secaoAtiva } = useOutletContext<{ secaoAtiva: Secao }>()
  const Icone = secaoMap[secaoAtiva]

  if (secaoAtiva === 'transacoes') return null
  return (
    <div
      className="painel"
      style={{ background: 'var(--primario-suave)', border: 'none' }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.8rem' }}>
        <Icone />
        <div>
          <div style={{ fontWeight: 700 }}>Módulo &ldquo;{secaoAtiva}&rdquo;</div>
          <div className="painel-sub">
            O design desta seção ainda não foi implementado.
          </div>
        </div>
      </div>
    </div>
  )
}

/* ---------- Modal de nova transação (POST) ---------- */

/* ---------- Saldo inicial da conta ---------- */

interface ModalSaldoInicialProps {
  /** Conta escolhida no botão "Saldo inicial"; null = fechado */
  conta: ContaResponse | null
  aoFechar: () => void
  aoSalvar: () => void
}

function ModalSaldoInicial({ conta, aoFechar, aoSalvar }: ModalSaldoInicialProps) {
  const [valor, setValor] = useState('')
  const [erro, setErro] = useState('')
  const [salvando, setSalvando] = useState(false)

  useEffect(() => {
    if (!conta) return
    setValor('')
    setErro('')
  }, [conta])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!conta) return
    setErro('')
    setSalvando(true)
    try {
      await definirSaldoInicial(conta.id, { valor: Number(valor) })
      aoSalvar()
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível salvar o saldo inicial.'))
    } finally {
      setSalvando(false)
    }
  }

  return (
    <Modal
      aberto={conta !== null}
      titulo="Saldo inicial"
      subtitulo={
        conta
          ? `Quanto a conta "${conta.nome}" tinha antes de todas as transações que você vai lançar nela.`
          : undefined
      }
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Valor (R$)
          <input
            type="number"
            step="0.01"
            value={valor}
            onChange={(e) => setValor(e.target.value)}
            placeholder="0,00"
            required
            autoFocus
          />
        </label>
        <p className="formulario-dica">
          Se for lançar transações de datas passadas (ou uma série que começou antes), use o saldo
          de antes delas, para não contar o mesmo valor duas vezes. Pode ser negativo ou zero.
          Depois de salvo, não dá para mudar: um erro se corrige com uma transação de ajuste.
        </p>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primario" disabled={salvando}>
            {salvando ? 'Salvando...' : 'Salvar saldo inicial'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

interface ModalNovaTransacaoProps {
  aberto: boolean
  aoFechar: () => void
  aoCriada?: () => void
}

function ModalNovaTransacao({ aberto, aoFechar, aoCriada }: ModalNovaTransacaoProps) {
  const [contas, setContas] = useState<OpcaoConta[]>([])
  const [categorias, setCategorias] = useState<CategoriaResponse[]>([])

  // Chave da conta escolhida: "c:ID" (individual) ou "cc:ID" (conjunta)
  const [contaChave, setContaChave] = useState('')
  const [tipo, setTipo] = useState<number>(TipoTransacao.SAIDA)
  const [valor, setValor] = useState('')
  const [descricao, setDescricao] = useState('')
  const [data, setData] = useState(() => dataLocalISO())
  const [categoriaId, setCategoriaId] = useState('')
  const [subCategoriaId, setSubCategoriaId] = useState('')
  const [recorrente, setRecorrente] = useState(false)
  const [periodicidade, setPeriodicidade] = useState<number>(PeriodicidadeTransacao.MENSAL)
  // Quantidade de vezes da série (vazio = sem fim)
  const [quantidade, setQuantidade] = useState('')
  const [status, setStatus] = useState<number>(StatusTransacao.CONCLUIDA)

  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const carregarContas = useCallback(async () => {
    try {
      const opcoes = await carregarOpcoesConta()
      setContas(opcoes)
      // Começa na conta padrão das Preferências ou na principal; se ela não estiver liberada
      // (sem saldo inicial ou excluída), na primeira conta liberada
      const liberadas = opcoes.filter((o) => o.liberada)
      const { contaPadrao } = preferenciasUso()
      const inicial =
        liberadas.find((o) => o.chave === contaPadrao) ??
        liberadas.find((o) => o.principal) ??
        liberadas[0]
      setContaChave(inicial ? inicial.chave : '')
    } catch {
      setErro('Não foi possível carregar as contas.')
    }
  }, [])

  const contaSelecionada = contas.find((o) => o.chave === contaChave)

  useEffect(() => {
    if (!aberto) return
    setValor('')
    setDescricao('')
    setCategoriaId('')
    setSubCategoriaId('')
    setTipo(TipoTransacao.SAIDA)
    setRecorrente(false)
    setPeriodicidade(PeriodicidadeTransacao.MENSAL)
    setQuantidade('')
    setStatus(StatusTransacao.CONCLUIDA)
    setData(dataLocalISO())
    setErro('')
    carregarContas()
  }, [aberto, carregarContas])

  // Carrega as categorias somente da conta selecionada (individual ou conjunta)
  useEffect(() => {
    if (!aberto || !contaSelecionada) {
      setCategorias([])
      return
    }
    let cancelado = false
    setErro('')
    ;(async () => {
      try {
        const categoriasData = contaSelecionada.conjunta
          ? await listarCategoriasPorContaConjunta(contaSelecionada.id)
          : await listarCategoriasPorConta(contaSelecionada.id)
        if (!cancelado) setCategorias(categoriasData)
      } catch (err: unknown) {
        if (!cancelado) setErro(extrairErro(err, 'Não foi possível carregar as categorias.'))
      }
    })()
    return () => {
      cancelado = true
    }
  }, [aberto, contaSelecionada])

  const categoriasFiltradas = categorias.filter((c) => c.tipo === tipo)
  const categoriaSelecionada = categorias.find((c) => c.id === Number(categoriaId))
  const subCategorias = categoriaSelecionada?.subCategorias ?? []

  // Ao trocar o tipo, limpa categoria/subcategoria selecionadas
  useEffect(() => {
    setCategoriaId('')
    setSubCategoriaId('')
  }, [tipo])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErro('')
    setCarregando(true)
    try {
      const request: CriarTransacaoRequest = {
        valor: Number(valor),
        descricao: descricao.trim() || null,
        dataTransacao: data,
        tipo: tipo as 0 | 1,
        // Classificação derivada: recorrente -> Fixa; não-recorrente -> Variável
        classificacao: recorrente
          ? ClassificacaoTransacao.FIXA
          : ClassificacaoTransacao.VARIAVEL,
        recorrente,
        periodicidade: recorrente ? (periodicidade as 0 | 1) : null,
        quantidadeOcorrencias: recorrente && quantidade ? Number(quantidade) : null,
        status: status as 0 | 1 | 2,
        categoriaId: categoriaId ? Number(categoriaId) : null,
        subCategoriaId: subCategoriaId ? Number(subCategoriaId) : null,
        // Exatamente uma: conta individual OU conta conjunta
        ...(contaSelecionada ? filtroDaConta(contaSelecionada) : {}),
      }
      await criarTransacao(request)
      aoCriada?.()
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível criar a transação.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Nova Transação"
      subtitulo="Registre uma movimentação em uma de suas contas."
      aoFechar={aoFechar}
      largura="md"
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <div className="campo">
          <span className="campo-rotulo">Tipo</span>
          <div className="btn-grupo" role="group" aria-label="Tipo da transação">
            <button
              type="button"
              className={tipo === TipoTransacao.SAIDA ? 'ativo' : ''}
              onClick={() => setTipo(TipoTransacao.SAIDA)}
            >
              Despesa
            </button>
            <button
              type="button"
              className={tipo === TipoTransacao.ENTRADA ? 'ativo' : ''}
              onClick={() => setTipo(TipoTransacao.ENTRADA)}
            >
              Receita
            </button>
          </div>
        </div>

        <label className="campo">
          Conta
          <select
            value={contaChave}
            onChange={(e) => {
              setContaChave(e.target.value)
              setCategoriaId('')
              setSubCategoriaId('')
            }}
            required
          >
            <OpcoesDeConta opcoes={contas} paraLancamento />
          </select>
        </label>

        <div className="formulario-dupla">
          <label className="campo">
            Valor (R$)
            <input
              type="number"
              min="0.01"
              step="0.01"
              value={valor}
              onChange={(e) => setValor(e.target.value)}
              placeholder="0,00"
              required
            />
          </label>
          <label className="campo">
            Data
            <input
              type="date"
              value={data}
              onChange={(e) => setData(e.target.value)}
              required
            />
          </label>
        </div>

        <label className="campo">
          Descrição
          <input
            type="text"
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
            placeholder="Ex: Supermercado, Salário..."
            maxLength={500}
          />
        </label>

        <label className="campo">
          Categoria
          <select
            value={categoriaId}
            onChange={(e) => {
              setCategoriaId(e.target.value)
              setSubCategoriaId('')
            }}
          >
            <option value="">Sem categoria</option>
            {categoriasFiltradas.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nome}
              </option>
            ))}
          </select>
        </label>

        <label className="campo">
          Subcategoria
          <select
            value={subCategoriaId}
            onChange={(e) => setSubCategoriaId(e.target.value)}
            disabled={!categoriaSelecionada || subCategorias.length === 0}
          >
            <option value="">
              {categoriaSelecionada && subCategorias.length === 0
                ? 'Sem subcategorias'
                : 'Sem subcategoria'}
            </option>
            {subCategorias.map((s: SubCategoriaResponse) => (
              <option key={s.id} value={s.id}>
                {s.nome}
              </option>
            ))}
          </select>
        </label>

        <label className="campo-check">
          <input
            type="checkbox"
            checked={recorrente}
            onChange={(e) => setRecorrente(e.target.checked)}
          />
          <span>Transação recorrente</span>
        </label>

        {recorrente && (
          <label className="campo">
            Periodicidade
            <select
              value={periodicidade}
              onChange={(e) => setPeriodicidade(Number(e.target.value))}
            >
              <option value={PeriodicidadeTransacao.MENSAL}>Mensal</option>
              <option value={PeriodicidadeTransacao.SEMANAL}>Semanal</option>
            </select>
          </label>
        )}

        {recorrente && (
          <label className="campo">
            Quantidade de vezes (opcional)
            <input
              type="number"
              min="2"
              max="120"
              step="1"
              value={quantidade}
              onChange={(e) => setQuantidade(e.target.value)}
              placeholder="Sem fim"
            />
          </label>
        )}

        <label className="campo">
          Status
          <select value={status} onChange={(e) => setStatus(Number(e.target.value))}>
            <option value={StatusTransacao.CONCLUIDA}>Concluída</option>
            <option value={StatusTransacao.PENDENTE}>Pendente</option>
            <option value={StatusTransacao.ATRASADA}>Atrasada</option>
          </select>
        </label>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primario" disabled={carregando}>
            {carregando ? 'Salvando...' : 'Salvar Transação'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

/* ---------- Modal de gerenciamento de transações (GET/PUT/DELETE, sem POST) ---------- */

interface ModalGerenciarTransacoesProps {
  aberto: boolean
  aoFechar: () => void
  aoAlterada?: () => void
}

const STATUS_NOME: Record<StatusTransacao, string> = {
  [StatusTransacao.PENDENTE]: 'Pendente',
  [StatusTransacao.CONCLUIDA]: 'Concluída',
  [StatusTransacao.ATRASADA]: 'Atrasada',
}

function ModalGerenciarTransacoes({
  aberto,
  aoFechar,
  aoAlterada,
}: ModalGerenciarTransacoesProps) {
  const [contas, setContas] = useState<OpcaoConta[]>([])
  // Chave da conta escolhida: "c:ID" (individual) ou "cc:ID" (conjunta)
  const [contaChave, setContaChave] = useState('')
  const [transacoes, setTransacoes] = useState<TransacaoResponse[]>([])
  const [carregando, setCarregando] = useState(false)
  const [erro, setErro] = useState('')

  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [editandoDescricao, setEditandoDescricao] = useState('')
  const [editandoStatus, setEditandoStatus] = useState<StatusTransacao>(StatusTransacao.CONCLUIDA)
  const [editandoBtn, setEditandoBtn] = useState(false)

  const [excluindoId, setExcluindoId] = useState<number | null>(null)
  const [excluindoBtn, setExcluindoBtn] = useState(false)

  // Ocorrência de série com a confirmação de "Parar de repetir" aberta
  const [parandoId, setParandoId] = useState<number | null>(null)
  const [parandoBtn, setParandoBtn] = useState(false)

  const carregarContas = useCallback(async () => {
    try {
      const lista = await carregarOpcoesConta()
      setContas(lista)
      const principal = lista.find((o) => o.principal)
      setContaChave(principal ? principal.chave : (lista[0]?.chave ?? ''))
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível carregar as contas.'))
    }
  }, [])

  const contaSelecionada = contas.find((o) => o.chave === contaChave)

  const carregarTransacoes = useCallback(
    async (conta: OpcaoConta | undefined) => {
      if (!conta) return
      setCarregando(true)
      setErro('')
      try {
        const lista = await listarTransacoes(filtroDaConta(conta))
        setTransacoes(lista)
      } catch (err: unknown) {
        setErro(extrairErro(err, 'Não foi possível carregar as transações.'))
      } finally {
        setCarregando(false)
      }
    },
    [],
  )

  useEffect(() => {
    if (!aberto) return
    setEditandoId(null)
    setExcluindoId(null)
    setParandoId(null)
    setErro('')
    carregarContas()
  }, [aberto, carregarContas])

  useEffect(() => {
    if (!aberto) return
    setEditandoId(null)
    setExcluindoId(null)
    setParandoId(null)
    carregarTransacoes(contaSelecionada)
  }, [aberto, contaSelecionada, carregarTransacoes])

  const iniciarEdicao = (t: TransacaoResponse) => {
    setExcluindoId(null)
    setParandoId(null)
    setEditandoId(t.id)
    setEditandoDescricao(t.descricao ?? '')
    setEditandoStatus(t.status)
    setErro('')
  }

  const salvarEdicao = async () => {
    if (editandoId === null) return
    setErro('')
    setEditandoBtn(true)
    try {
      const request: EditarTransacaoRequest = {
        descricao: editandoDescricao.trim() || null,
        status: editandoStatus,
      }
      const atualizada = await editarTransacao(editandoId, request)
      setTransacoes((prev) => prev.map((t) => (t.id === atualizada.id ? atualizada : t)))
      aoAlterada?.()
      setEditandoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível editar a transação.'))
    } finally {
      setEditandoBtn(false)
    }
  }

  const pedirExclusao = (id: number) => {
    setEditandoId(null)
    setParandoId(null)
    setExcluindoId((prev) => (prev === id ? null : id))
    setErro('')
  }

  // Em ocorrência de série, PROXIMAS exclui também as seguintes não pagas (pendentes ou atrasadas) e para a série
  const confirmarExclusao = async (escopo: EscopoExclusao = EscopoExclusao.ESTA) => {
    if (excluindoId === null) return
    setErro('')
    setExcluindoBtn(true)
    try {
      await excluirTransacao(excluindoId, escopo)
      if (escopo === EscopoExclusao.PROXIMAS) await carregarTransacoes(contaSelecionada)
      else setTransacoes((prev) => prev.filter((t) => t.id !== excluindoId))
      aoAlterada?.()
      setExcluindoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível excluir a transação.'))
    } finally {
      setExcluindoBtn(false)
    }
  }

  const pedirParada = (id: number) => {
    setEditandoId(null)
    setExcluindoId(null)
    setParandoId((prev) => (prev === id ? null : id))
    setErro('')
  }

  // A série para de repetir: recarrega para todas as ocorrências mostrarem "Encerrada"
  const confirmarParada = async () => {
    if (parandoId === null) return
    setErro('')
    setParandoBtn(true)
    try {
      await pararRecorrencia(parandoId)
      await carregarTransacoes(contaSelecionada)
      aoAlterada?.()
      setParandoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível parar a série.'))
    } finally {
      setParandoBtn(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Transações"
      subtitulo="Edite a descrição e o status ou exclua suas transações por conta."
      aoFechar={aoFechar}
      largura="lg"
    >
      <div className="gerenciar-sub">
        <label className="campo">
          Conta
          <select
            value={contaChave}
            onChange={(e) => setContaChave(e.target.value)}
            disabled={contas.length === 0}
            required
          >
            {contas.length === 0 && <option value="">Carregando contas...</option>}
            <OpcoesDeConta opcoes={contas} />
          </select>
        </label>

        {erro && <p className="erro">{erro}</p>}

        <div className="gerenciar-sub-lista">
          {carregando ? (
            <div className="estado-vazio">Carregando transações...</div>
          ) : transacoes.length === 0 ? (
            <div className="estado-vazio">
              Nenhuma transação nesta conta.
              <button
                type="button"
                className="btn btn-ghost"
                onClick={() => carregarTransacoes(contaSelecionada)}
              >
                Atualizar
              </button>
            </div>
          ) : (
            transacoes.map((t) => (
              <div key={t.id} className="gerenciar-sub-item">
                {editandoId === t.id ? (
                  <div className="gerenciar-sub-editar">
                    <input
                      type="text"
                      value={editandoDescricao}
                      onChange={(e) => setEditandoDescricao(e.target.value)}
                      placeholder="Descrição"
                      maxLength={500}
                      autoFocus
                    />
                    <select
                      value={editandoStatus}
                      onChange={(e) => setEditandoStatus(Number(e.target.value) as StatusTransacao)}
                      title={t.dataTransacao > dataLocalISO() ? 'Data futura: só fica pendente até chegar o dia' : undefined}
                    >
                      {/* Data futura: não dá para pagar (nem atrasar) o que ainda não chegou */}
                      <option value={StatusTransacao.CONCLUIDA} disabled={t.dataTransacao > dataLocalISO()}>
                        Concluída
                      </option>
                      <option value={StatusTransacao.PENDENTE}>Pendente</option>
                      <option value={StatusTransacao.ATRASADA} disabled={t.dataTransacao > dataLocalISO()}>
                        Atrasada
                      </option>
                    </select>
                    <button
                      type="button"
                      className="btn btn-primario"
                      onClick={salvarEdicao}
                      disabled={editandoBtn}
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
                ) : (
                  <>
                    <div className="gerenciar-sub-info">
                      <span className="gerenciar-sub-nome">{t.descricao || 'Sem descrição'}</span>
                      <span className="gerenciar-sub-conta">
                        {formatarData(t.dataTransacao)} · {moedaSimples(t.valor)}
                      </span>
                      {t.recorrenciaId != null && (
                        <span className="td-tags">
                          <TagsRecorrencia etiqueta={etiquetaRecorrencia(t)} />
                        </span>
                      )}
                    </div>
                    <div className="gerenciar-sub-acoes">
                      <span
                        className={`tag ${
                          t.status === StatusTransacao.ATRASADA
                            ? 'vermelho'
                            : t.status === StatusTransacao.PENDENTE
                              ? 'ambar'
                              : 'verde'
                        }`}
                      >
                        {STATUS_NOME[t.status]}
                      </span>
                      <button
                        type="button"
                        className="gerenciar-sub-icone"
                        aria-label="Editar"
                        title="Editar"
                        onClick={() => iniciarEdicao(t)}
                      >
                        <IconeLapis />
                      </button>
                      {t.serieAtiva === true && (
                        <button
                          type="button"
                          className="gerenciar-sub-icone"
                          aria-label="Parar de repetir"
                          title="Parar de repetir"
                          onClick={() => pedirParada(t.id)}
                        >
                          <IconeRepetirParar />
                        </button>
                      )}
                      <button
                        type="button"
                        className="gerenciar-sub-icone perigo"
                        aria-label="Excluir"
                        title="Excluir"
                        onClick={() => pedirExclusao(t.id)}
                      >
                        <IconeLixeira />
                      </button>
                    </div>
                  </>
                )}

                {parandoId === t.id && (
                  <div className="gerenciar-sub-confirma">
                    <div className="gerenciar-sub-confirma-texto">
                      <span>Parar de repetir esta série?</span>
                      <span className="gerenciar-sub-confirma-adendo">
                        As ocorrências já lançadas continuam como estão.
                      </span>
                    </div>
                    <div className="gerenciar-sub-confirma-acoes">
                      <button
                        type="button"
                        className="btn btn-perigo"
                        onClick={confirmarParada}
                        disabled={parandoBtn}
                      >
                        {parandoBtn ? 'Parando...' : 'Parar'}
                      </button>
                      <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={() => setParandoId(null)}
                      >
                        Cancelar
                      </button>
                    </div>
                  </div>
                )}

                {excluindoId === t.id && (
                  <div className="gerenciar-sub-confirma">
                    {t.recorrenciaId != null ? (
                      <div className="gerenciar-sub-confirma-texto">
                        <span>Excluir esta ocorrência da série?</span>
                        <span className="gerenciar-sub-confirma-adendo">
                          &ldquo;Esta e as próximas&rdquo; exclui também as seguintes ainda não pagas e
                          para a série.
                        </span>
                      </div>
                    ) : (
                      <span>Excluir esta transação?</span>
                    )}
                    <div className="gerenciar-sub-confirma-acoes">
                      <button
                        type="button"
                        className="btn btn-perigo"
                        onClick={() => confirmarExclusao(EscopoExclusao.ESTA)}
                        disabled={excluindoBtn}
                      >
                        {excluindoBtn ? 'Excluindo...' : t.recorrenciaId != null ? 'Só esta' : 'Excluir'}
                      </button>
                      {t.recorrenciaId != null && (
                        <button
                          type="button"
                          className="btn btn-perigo"
                          onClick={() => confirmarExclusao(EscopoExclusao.PROXIMAS)}
                          disabled={excluindoBtn}
                        >
                          Esta e as próximas
                        </button>
                      )}
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
