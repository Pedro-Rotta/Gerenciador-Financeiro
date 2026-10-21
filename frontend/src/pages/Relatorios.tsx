/**
 * Relatorios — Seção de análise financeira.
 * Cards de resumo executivo, gráficos (evolução de receitas x despesas e
 * distribuição das despesas por categoria), Top 5 despesas e Comparativo mensal
 * (ambos com "Ver todas"), todos com dados reais do mês selecionado
 * (GET api/Relatorio/resumo).
 */

import { useCallback, useEffect, useRef, useState } from 'react'
import {
  IconeDownload,
  IconeFiltro,
  IconeSetaEsquerda,
  IconeSetaDireita,
} from '../components/icons'
import { gerarExcelRelatorio, nomeArquivoExcel } from '../utils/excelRelatorio'
import { baixarArquivo } from '../utils/download'
import { baixarPdfRelatorio, obterResumoRelatorio } from '../services/relatorios'
import type {
  MesesPeriodo,
  RelatorioDespesaItem,
  RelatorioMesItem,
  RelatorioResumoResponse,
} from '../types/relatorio'
import { dataLocalISO } from '../utils/data'
import { extrairErro } from '../utils/erros'
import { preferenciasUso } from '../utils/preferenciasUso'
/* ---------- Helpers ---------- */

const MESES = ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez']

// Linhas exibidas nas tabelas antes de "Ver todas"
const LIMITE_TOP_DESPESAS = 5
const LIMITE_COMPARATIVO = 6

// Meses exibidos no gráfico de evolução (terminando no mês do relatório)
const MESES_EVOLUCAO = 6
// Fatias da rosca: as maiores categorias; o restante vira "Outros"
const MAX_FATIAS = 6
// Cores categóricas da rosca (variáveis --serie-N em index.css, validadas em claro/escuro)
const SLOTS_SERIE = 6

function moeda(valor: number) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

// Valor compacto para eixos e rótulos: "R$ 9,1 mil"
function moedaCompacta(valor: number) {
  return valor.toLocaleString('pt-BR', {
    style: 'currency',
    currency: 'BRL',
    notation: 'compact',
    maximumFractionDigits: 1,
  })
}

// Arredonda para cima até um número "redondo" (1, 2, 2,5, 5 ou 10 × 10^n)
function numeroRedondo(valor: number) {
  if (valor <= 0) return 1
  const potencia = 10 ** Math.floor(Math.log10(valor))
  const fracao = valor / potencia
  const redondo = fracao <= 1 ? 1 : fracao <= 2 ? 2 : fracao <= 2.5 ? 2.5 : fracao <= 5 ? 5 : 10
  return redondo * potencia
}

function percentual(valor: number) {
  return `${valor.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`
}

// "Out/26" a partir do ano e do mês (1-12)
function rotuloMes(ano: number, mes: number) {
  return `${MESES[mes - 1]}/${String(ano).slice(-2)}`
}

// Opções do seletor de período (meses terminando no mês escolhido no calendário)
const PERIODOS_RELATORIO: { meses: MesesPeriodo; rotulo: string }[] = [
  { meses: 1, rotulo: 'Mês' },
  { meses: 3, rotulo: 'Trimestre' },
  { meses: 6, rotulo: 'Semestre' },
  { meses: 12, rotulo: 'Ano' },
]

// "Out/26" (um mês) ou "Ago/26 – Out/26" (trimestre, semestre, ano).
// O início é calculado do último mês e da quantidade de meses (não depende de dataInicio).
function rotuloPeriodo(r: RelatorioResumoResponse) {
  if (r.meses === 1) return rotuloMes(r.ano, r.mes)
  const inicio = new Date(r.ano, r.mes - r.meses, 1)
  return `${rotuloMes(inicio.getFullYear(), inicio.getMonth() + 1)} – ${rotuloMes(r.ano, r.mes)}`
}

// "mês" ou "período", para os textos dos cards e tabelas
function nomePeriodo(meses: number) {
  return meses === 1 ? 'mês' : 'período'
}

// Mês atual no formato do <input type="month">: "yyyy-MM"
function mesAtualIso() {
  return dataLocalISO().slice(0, 7)
}

// Soma (ou subtrai) meses de um "yyyy-MM"
function somarMeses(mesIso: string, quantidade: number) {
  const [ano, mes] = mesIso.split('-').map(Number)
  const data = new Date(ano, mes - 1 + quantidade, 1)
  return `${data.getFullYear()}-${String(data.getMonth() + 1).padStart(2, '0')}`
}

/* ---------- Gráfico de evolução (linhas, SVG) ---------- */

const SERIES_EVOLUCAO = [
  { chave: 'receita', nome: 'Receitas', cor: 'var(--grafico-receita)' },
  { chave: 'despesa', nome: 'Despesas', cor: 'var(--grafico-despesa)' },
] as const

/**
 * Receitas x despesas por mês (os últimos meses do comparativo, terminando no mês
 * do relatório). Passar o mouse ou focar um mês mostra o crosshair e o tooltip com
 * as duas séries; os mesmos valores estão na tabela "Comparativo Mensal".
 */
function GraficoEvolucao({ meses, quantidade }: { meses: RelatorioMesItem[]; quantidade: number }) {
  const [ativo, setAtivo] = useState<number | null>(null)
  const dados = meses.slice(-quantidade)

  // O SVG acompanha a largura real do painel (escala 1:1), para o texto dos
  // eixos manter o tamanho de verdade no celular em vez de encolher junto
  const wrapRef = useRef<HTMLDivElement>(null)
  const [largura, setLargura] = useState(560)
  useEffect(() => {
    const el = wrapRef.current
    if (!el) return
    const observador = new ResizeObserver(([entrada]) => {
      setLargura(Math.max(Math.round(entrada.contentRect.width), 280))
    })
    observador.observe(el)
    return () => observador.disconnect()
  }, [])

  if (dados.length === 0 || dados.every((m) => m.receita === 0 && m.despesa === 0)) {
    return (
      <div ref={wrapRef} className="rel-grafico-wrap">
        <div className="estado-vazio">Nenhuma movimentação nos últimos meses.</div>
      </div>
    )
  }

  const altura = 240
  const padEsq = 64
  const padDir = 24
  const padTop = 24
  const padBase = 36

  // Eixo Y com 4 intervalos "redondos" cobrindo o maior valor
  const maior = Math.max(...dados.map((m) => Math.max(m.receita, m.despesa)))
  const passo = numeroRedondo(maior / 4)
  const topo = passo * 4

  const larguraUtil = largura - padEsq - padDir
  const px = (i: number) =>
    dados.length === 1 ? padEsq + larguraUtil / 2 : padEsq + (i / (dados.length - 1)) * larguraUtil
  const py = (v: number) => padTop + (1 - v / topo) * (altura - padTop - padBase)
  const base = altura - padBase

  // Faixa de hover de cada mês (alvo maior que a marca)
  const faixa = dados.length === 1 ? larguraUtil : larguraUtil / (dados.length - 1)

  // Rótulos diretos no último mês: a série mais alta acima do ponto, a outra abaixo
  const ultimo = dados.length - 1
  const receitaAcima = dados[ultimo].receita >= dados[ultimo].despesa
  const mesAtivo = ativo !== null ? dados[ativo] : null

  return (
    <div ref={wrapRef} className="rel-grafico-wrap" onMouseLeave={() => setAtivo(null)}>
      <svg
        viewBox={`0 0 ${largura} ${altura}`}
        className="rel-grafico-svg"
        role="img"
        aria-label="Evolução de receitas e despesas por mês"
      >
        {/* Grade e eixo Y */}
        {[0, 1, 2, 3, 4].map((n) => {
          const y = py(passo * n)
          return (
            <g key={n}>
              <line x1={padEsq} x2={largura - padDir} y1={y} y2={y} className={n === 0 ? 'eixo' : 'grid'} />
              <text x={padEsq - 8} y={y + 4} className="eixo-label" textAnchor="end">
                {n === 0 ? 'R$ 0' : moedaCompacta(passo * n)}
              </text>
            </g>
          )
        })}

        {/* Rótulos dos meses */}
        {dados.map((m, i) => (
          <text key={`${m.ano}-${m.mes}`} x={px(i)} y={altura - 14} className="eixo-label" textAnchor="middle">
            {rotuloMes(m.ano, m.mes)}
          </text>
        ))}

        {/* Crosshair */}
        {ativo !== null && <line x1={px(ativo)} x2={px(ativo)} y1={padTop} y2={base} className="crosshair" />}

        {/* Linhas e marcadores (anel na cor da superfície) */}
        {SERIES_EVOLUCAO.map((s) => (
          <g key={s.chave}>
            <polyline
              points={dados.map((m, i) => `${px(i)},${py(m[s.chave])}`).join(' ')}
              fill="none"
              stroke={s.cor}
              strokeWidth="2"
              strokeLinejoin="round"
              strokeLinecap="round"
            />
            {dados.map((m, i) => (
              <circle
                key={i}
                cx={px(i)}
                cy={py(m[s.chave])}
                r={ativo === i ? 5 : 4}
                fill={s.cor}
                className="marcador"
              />
            ))}
          </g>
        ))}

        {/* Rótulos diretos do último mês */}
        {SERIES_EVOLUCAO.map((s) => {
          const acima = s.chave === 'receita' ? receitaAcima : !receitaAcima
          return (
            <text
              key={s.chave}
              x={px(ultimo)}
              y={py(dados[ultimo][s.chave]) + (acima ? -10 : 18)}
              className="valor-label"
              textAnchor={dados.length === 1 ? 'middle' : 'end'}
            >
              {moedaCompacta(dados[ultimo][s.chave])}
            </text>
          )
        })}

        {/* Alvos de hover/foco: uma faixa por mês */}
        {dados.map((m, i) => (
          <rect
            key={`alvo-${m.ano}-${m.mes}`}
            x={px(i) - faixa / 2}
            y={padTop}
            width={faixa}
            height={base - padTop}
            className="alvo"
            tabIndex={0}
            aria-label={`${rotuloMes(m.ano, m.mes)}: receitas ${moeda(m.receita)}, despesas ${moeda(m.despesa)}`}
            onMouseEnter={() => setAtivo(i)}
            onFocus={() => setAtivo(i)}
            onBlur={() => setAtivo(null)}
          />
        ))}
      </svg>

      {mesAtivo && ativo !== null && (
        <div
          className={`rel-tooltip ${px(ativo) > largura / 2 ? 'esquerda' : ''}`}
          style={{ left: `${(px(ativo) / largura) * 100}%` }}
          role="status"
        >
          <div className="rel-tooltip-titulo">{rotuloMes(mesAtivo.ano, mesAtivo.mes)}</div>
          {SERIES_EVOLUCAO.map((s) => (
            <div key={s.chave} className="rel-tooltip-linha">
              <span className="rel-tooltip-chave" style={{ background: s.cor }} />
              <strong>{moeda(mesAtivo[s.chave])}</strong>
              <span>{s.nome}</span>
            </div>
          ))}
          <div className="rel-tooltip-linha resultado">
            <strong>{moeda(mesAtivo.resultado)}</strong>
            <span>Resultado</span>
          </div>
        </div>
      )}
    </div>
  )
}

/* ---------- Gráfico de rosca (distribuição das despesas) ---------- */

interface FatiaCategoria {
  nome: string
  valor: number
  cor: string
}

// Slot de cor estável pelo nome: a mesma categoria mantém a cor ao trocar de mês
// (a cor segue a categoria, não a posição no ranking). Colisões vão para o próximo slot livre.
function coresPorCategoria(nomes: string[]): Map<string, string> {
  const usados = new Set<number>()
  const cores = new Map<string, string>()
  for (const nome of nomes) {
    let slot = nome.split('').reduce((acc, c) => (acc * 31 + c.charCodeAt(0)) % 9973, 7) % SLOTS_SERIE
    while (usados.has(slot)) slot = (slot + 1) % SLOTS_SERIE
    usados.add(slot)
    cores.set(nome, `var(--serie-${slot + 1})`)
  }
  return cores
}

// Soma as despesas por categoria; acima de MAX_FATIAS, as menores viram "Outros"
function montarFatias(despesas: RelatorioDespesaItem[]): FatiaCategoria[] {
  const porCategoria = new Map<string, number>()
  despesas.forEach((d) => porCategoria.set(d.categoria, (porCategoria.get(d.categoria) ?? 0) + d.valor))
  const ordenadas = Array.from(porCategoria.entries()).sort((a, b) => b[1] - a[1])

  const principais = ordenadas.length > MAX_FATIAS ? ordenadas.slice(0, MAX_FATIAS - 1) : ordenadas
  const resto = ordenadas.slice(principais.length).reduce((acc, [, v]) => acc + v, 0)

  const cores = coresPorCategoria(principais.map(([nome]) => nome))
  const fatias = principais.map(([nome, valor]) => ({ nome, valor, cor: cores.get(nome)! }))
  if (resto > 0) fatias.push({ nome: 'Outros', valor: resto, cor: 'var(--serie-outros)' })
  return fatias
}

// Valor no centro da rosca: compacto a partir de R$ 100 mil para caber no furo
function valorCentro(valor: number) {
  return valor >= 100_000 ? moedaCompacta(valor) : moeda(valor)
}

/**
 * Despesas do mês por categoria. Passar o mouse/focar uma fatia ou um item da
 * legenda destaca a categoria e mostra o valor no centro; a legenda lista todas.
 */
function GraficoDistribuicao({ despesas }: { despesas: RelatorioDespesaItem[] }) {
  const [ativa, setAtiva] = useState<string | null>(null)
  const fatias = montarFatias(despesas)
  const total = fatias.reduce((acc, f) => acc + f.valor, 0)

  if (total <= 0) {
    return <div className="estado-vazio">Nenhuma despesa no período.</div>
  }

  const raio = 60
  const espessura = 18
  const circunf = 2 * Math.PI * raio
  // 2px de superfície entre fatias (nenhum quando há uma só)
  const espaco = fatias.length > 1 ? 2 : 0
  const fatiaAtiva = fatias.find((f) => f.nome === ativa) ?? null

  let acumulado = 0

  return (
    <div className="rel-roca-wrap" onMouseLeave={() => setAtiva(null)}>
      <div className="rel-roca">
        <svg viewBox="0 0 160 160" className="rel-roca-svg" role="img" aria-label="Distribuição das despesas por categoria">
          <circle cx="80" cy="80" r={raio} fill="none" className="rel-roca-trilho" strokeWidth={espessura} />
          {fatias.map((f) => {
            const tamanho = (f.valor / total) * circunf
            const inicio = acumulado
            acumulado += tamanho
            return (
              <circle
                key={f.nome}
                cx="80"
                cy="80"
                r={raio}
                fill="none"
                stroke={f.cor}
                strokeWidth={espessura}
                strokeDasharray={`${Math.max(tamanho - espaco, 0.5)} ${circunf - Math.max(tamanho - espaco, 0.5)}`}
                strokeDashoffset={-inicio}
                transform="rotate(-90 80 80)"
                className={`rel-roca-fatia ${ativa && ativa !== f.nome ? 'apagada' : ''}`}
                onMouseEnter={() => setAtiva(f.nome)}
              />
            )
          })}
        </svg>
        <div className="rel-roca-centro">
          <span className="rel-roca-total">{valorCentro(fatiaAtiva ? fatiaAtiva.valor : total)}</span>
          <span className="rel-roca-sub">
            {fatiaAtiva
              ? `${fatiaAtiva.nome} · ${percentual((fatiaAtiva.valor / total) * 100)}`
              : 'Total de despesas'}
          </span>
        </div>
      </div>

      <div className="rel-roca-legenda">
        {fatias.map((f) => (
          <div
            className={`rel-roca-legend-item ${ativa && ativa !== f.nome ? 'apagada' : ''}`}
            key={f.nome}
            tabIndex={0}
            onMouseEnter={() => setAtiva(f.nome)}
            onFocus={() => setAtiva(f.nome)}
            onBlur={() => setAtiva(null)}
          >
            <span className="rel-roca-dot" style={{ background: f.cor }} />
            <span className="rel-roca-legend-nome">{f.nome}</span>
            <span className="rel-roca-legend-pct">{((f.valor / total) * 100).toFixed(0)}%</span>
            <span className="rel-roca-legend-valor">{moeda(f.valor)}</span>
          </div>
        ))}
      </div>
    </div>
  )
}


/* ---------- Cards de resumo executivo ---------- */

interface ResumoCard {
  rotulo: string
  valor: string
  comparativo?: string
  positivo?: boolean
  sub?: string
}

function ResumoCardFund({ rotulo, valor, comparativo, positivo, sub }: ResumoCard) {
  return (
    <div className="card-resumo">
      <div className="card-resumo-info" style={{ width: '100%' }}>
        <div className="card-resumo-rotulo">{rotulo}</div>
        <div className="card-resumo-valor">{valor}</div>
        {comparativo && !sub && (
          <div className={`card-resumo-tendencia ${positivo ? 'alta' : 'baixa'}`}>
            {positivo ? '▲' : '▼'} {comparativo}
          </div>
        )}
        {sub && <div className="rel-resumo-sub">{sub}</div>}
      </div>
    </div>
  )
}

// Monta os 4 cards do resumo executivo a partir do resumo do mês
function montarCards(r: RelatorioResumoResponse): ResumoCard[] {
  const periodo = nomePeriodo(r.meses)

  // 1. Média de gastos diários vs. período anterior de mesmo tamanho (gastar menos é positivo)
  const anterior = r.mediaGastosDiariosMesAnterior
  let cardMedia: ResumoCard = {
    rotulo: 'Média de Gastos Diários',
    valor: moeda(r.mediaGastosDiarios),
    sub: `Sem gastos no ${periodo} anterior`,
  }
  if (anterior > 0) {
    const variacao = ((r.mediaGastosDiarios - anterior) / anterior) * 100
    cardMedia = {
      ...cardMedia,
      sub: undefined,
      comparativo:
        Math.abs(variacao) < 0.05
          ? `igual ao ${periodo} anterior`
          : `${percentual(Math.abs(variacao))} ${variacao > 0 ? 'acima' : 'abaixo'} do ${periodo} anterior`,
      positivo: variacao <= 0,
    }
  }

  // 2. Economia do período: % da receita que sobrou
  const rotuloEconomia = r.meses === 1 ? 'Economia Mensal' : 'Economia no Período'
  const cardEconomia: ResumoCard =
    r.economiaPercentual === null
      ? { rotulo: rotuloEconomia, valor: '—', sub: `Sem receitas no ${periodo}` }
      : {
          rotulo: rotuloEconomia,
          valor: percentual(r.economiaPercentual),
          comparativo: r.economiaPercentual >= 0 ? 'da receita poupada' : 'gastou mais do que recebeu',
          positivo: r.economiaPercentual >= 0,
        }

  // 3. Maior categoria de gastos
  const cardCategoria: ResumoCard = {
    rotulo: 'Maior Categoria de Gastos',
    valor: r.maiorCategoriaNome ?? '—',
    sub: r.maiorCategoriaNome
      ? `${moeda(r.maiorCategoriaValor)} no período`
      : 'Nenhuma despesa no período',
  }

  // 4. Quanto guardar por mês para as metas vs. o que sobrou por mês (receitas - despesas;
  //    em período de vários meses, a média mensal)
  const sobra = (r.receitasMes - r.despesasMes) / r.meses
  const sufixoSobra = r.meses === 1 ? 'no mês' : 'por mês, em média'
  let cardMetas: ResumoCard = {
    rotulo: 'Guardar por Mês para Metas',
    valor: '—',
    sub: 'Nenhuma meta em andamento',
  }
  if (r.metasEmAndamento > 0) {
    cardMetas = {
      ...cardMetas,
      valor: moeda(r.aporteMensalMetas),
      sub: undefined,
      comparativo:
        sobra < 0
          ? `gastou ${moeda(-sobra)} a mais do que recebeu ${sufixoSobra}`
          : sobra >= r.aporteMensalMetas
            ? `sobraram ${moeda(sobra)} ${sufixoSobra}`
            : `sobraram ${moeda(sobra)} ${sufixoSobra}, faltam ${moeda(r.aporteMensalMetas - sobra)}`,
      positivo: sobra >= r.aporteMensalMetas,
    }
  }

  return [cardMedia, cardEconomia, cardCategoria, cardMetas]
}

/* ---------- Tabela de performance ---------- */

interface BotaoVerTodasProps {
  expandida: boolean
  rotulo: string
  aoAlternar: () => void
}

// Botão do cabeçalho do painel que expande/recolhe a tabela
function BotaoVerTodas({ expandida, rotulo, aoAlternar }: BotaoVerTodasProps) {
  return (
    <button type="button" className="btn-link rel-ver-todas" aria-expanded={expandida} onClick={aoAlternar}>
      {expandida ? 'Ver menos' : rotulo}
    </button>
  )
}

interface TabelaComparativoProps {
  linhas: RelatorioMesItem[]
  /** false: só os últimos `limite` meses; true: todos os meses */
  expandida: boolean
  carregando: boolean
  limite: number
}

function TabelaComparativo({ linhas, expandida, carregando, limite }: TabelaComparativoProps) {
  if (carregando) return <div className="estado-vazio">Carregando comparativo...</div>
  const visiveis = expandida ? linhas : linhas.slice(-limite)
  return (
    <div className="tabela-wrapper">
      <table className="tabela">
        <thead>
          <tr>
            <th>Mês</th>
            <th>Receita</th>
            <th>Despesa</th>
            <th>Resultado</th>
            <th>Variação %</th>
          </tr>
        </thead>
        <tbody>
          {visiveis.map((linha) => (
            <tr key={`${linha.ano}-${linha.mes}`}>
              <td className="tabela-rotulo">{rotuloMes(linha.ano, linha.mes)}</td>
              <td>{moeda(linha.receita)}</td>
              <td>{moeda(linha.despesa)}</td>
              <td className={linha.resultado >= 0 ? 'positivo' : 'negativo'}>
                {moeda(linha.resultado)}
              </td>
              <td
                className={
                  linha.variacaoPercentual === null
                    ? ''
                    : linha.variacaoPercentual >= 0
                      ? 'positivo'
                      : 'negativo'
                }
              >
                {linha.variacaoPercentual === null
                  ? '—'
                  : `${linha.variacaoPercentual > 0 ? '+' : ''}${percentual(linha.variacaoPercentual)}`}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

interface TabelaTopDespesasProps {
  itens: RelatorioDespesaItem[]
  /** false: só as 5 maiores; true: todas as despesas do mês */
  expandida: boolean
  carregando: boolean
}

function TabelaTopDespesas({ itens, expandida, carregando }: TabelaTopDespesasProps) {
  if (carregando) return <div className="estado-vazio">Carregando despesas...</div>
  if (itens.length === 0) return <div className="estado-vazio">Nenhuma despesa no período.</div>
  const visiveis = expandida ? itens : itens.slice(0, LIMITE_TOP_DESPESAS)
  return (
    <div className="rel-top-lista">
      {visiveis.map((item, i) => (
        <div className="rel-top-item" key={`${i}-${item.descricao}`}>
          <span className="rel-top-ranking">{i + 1}</span>
          <div className="rel-top-info">
            <div className="rel-top-nome">{item.descricao}</div>
            <div className="rel-top-cat">{item.categoria}</div>
          </div>
          <span className="rel-top-valor">{moeda(item.valor)}</span>
        </div>
      ))}
    </div>
  )
}

/* ---------- Barra de filtros ---------- */

interface BarraFiltrosProps {
  /** Último mês do período, no formato "yyyy-MM" */
  mes: string
  aoMudarMes: (mes: string) => void
  /** Tamanho do período em meses (mês, trimestre, semestre, ano) */
  meses: MesesPeriodo
  aoMudarMeses: (meses: MesesPeriodo) => void
  /** Baixa o PDF do período (gerado no backend); undefined enquanto não há dados */
  aoExportarPdf?: () => void
  /** true enquanto o PDF está sendo gerado */
  gerandoPdf: boolean
  /** Baixa o Excel (.xlsx) do período; undefined enquanto não há dados */
  aoExportarExcel?: () => void
  /** true enquanto o .xlsx está sendo gerado */
  gerandoExcel: boolean
}

function BarraFiltros({
  mes,
  aoMudarMes,
  meses,
  aoMudarMeses,
  aoExportarPdf,
  gerandoPdf,
  aoExportarExcel,
  gerandoExcel,
}: BarraFiltrosProps) {
  const mesMaximo = mesAtualIso()

  return (
    <div className="rel-filtros">
      <div className="rel-filtros-left">
        <span className="rel-filtros-icone"><IconeFiltro /></span>
        <select
          className="rel-select"
          aria-label="Período"
          value={meses}
          onChange={(e) => aoMudarMeses(Number(e.target.value) as MesesPeriodo)}
        >
          {PERIODOS_RELATORIO.map((p) => (
            <option key={p.meses} value={p.meses}>
              {p.rotulo}
            </option>
          ))}
        </select>

        {/* Período mais longo que um mês termina no mês escolhido */}
        {meses > 1 && <span className="rel-filtros-ate">até</span>}

        {/* Mês do relatório (não permite meses futuros) */}
        <div className="rel-mes">
          <button
            type="button"
            className="rel-mes-btn"
            aria-label="Mês anterior"
            onClick={() => aoMudarMes(somarMeses(mes, -1))}
          >
            <IconeSetaEsquerda />
          </button>
          <input
            type="month"
            className="rel-select rel-input-mes"
            value={mes}
            max={mesMaximo}
            onChange={(e) => {
              if (e.target.value && e.target.value <= mesMaximo) aoMudarMes(e.target.value)
            }}
            aria-label="Mês e ano"
          />
          <button
            type="button"
            className="rel-mes-btn"
            aria-label="Próximo mês"
            disabled={mes >= mesMaximo}
            onClick={() => aoMudarMes(somarMeses(mes, 1))}
          >
            <IconeSetaDireita />
          </button>
        </div>
      </div>

      <div className="rel-filtros-right">
        <button
          type="button"
          className="btn btn-ghost"
          onClick={aoExportarPdf}
          disabled={!aoExportarPdf || gerandoPdf}
          title="Baixa o relatório do período em PDF"
        >
          <IconeDownload />
          <span className="btn-label">{gerandoPdf ? 'Gerando...' : 'Exportar PDF'}</span>
        </button>
        <button
          type="button"
          className="btn btn-ghost"
          onClick={aoExportarExcel}
          disabled={!aoExportarExcel || gerandoExcel}
          title="Baixa uma planilha formatada com resumo, despesas e comparativo mensal"
        >
          <IconeDownload />
          <span className="btn-label">{gerandoExcel ? 'Gerando...' : 'Exportar Excel'}</span>
        </button>
      </div>
    </div>
  )
}

/* ---------- Página principal ---------- */

export default function Relatorios() {
  // Último mês do período ("yyyy-MM"; começa no mês atual) e tamanho do período
  const [mes, setMes] = useState(mesAtualIso)
  // Começa no período padrão das Preferências (1, 3, 6 ou 12 meses)
  const [meses, setMeses] = useState<MesesPeriodo>(() => preferenciasUso().periodoRelatorios as MesesPeriodo)
  const [resumo, setResumo] = useState<RelatorioResumoResponse | null>(null)
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState('')
  const [recarga, setRecarga] = useState(0)
  // "Ver todas" das tabelas
  const [despesasExpandidas, setDespesasExpandidas] = useState(false)
  const [comparativoExpandido, setComparativoExpandido] = useState(false)

  // GET — resumo do período selecionado (recarrega ao trocar o mês ou o período)
  useEffect(() => {
    let cancelado = false
    const [ano, numeroMes] = mes.split('-').map(Number)
    setCarregando(true)
    setErro('')
    obterResumoRelatorio(ano, numeroMes, meses)
      .then((data) => {
        if (!cancelado) setResumo(data)
      })
      .catch((err: unknown) => {
        if (!cancelado) setErro(extrairErro(err, 'Não foi possível carregar o relatório.'))
      })
      .finally(() => {
        if (!cancelado) setCarregando(false)
      })
    return () => {
      cancelado = true
    }
  }, [mes, meses, recarga])

  const tentarNovamente = useCallback(() => setRecarga((n) => n + 1), [])

  // Exportar Excel: gera o .xlsx formatado no navegador (ExcelJS carregado sob demanda)
  const [gerandoExcel, setGerandoExcel] = useState(false)
  const [erroExportacao, setErroExportacao] = useState('')
  const exportarExcel = async () => {
    if (!resumo) return
    setErroExportacao('')
    setGerandoExcel(true)
    try {
      const arquivo = await gerarExcelRelatorio(resumo, rotuloPeriodo(resumo))
      baixarArquivo(arquivo, nomeArquivoExcel(resumo))
    } catch {
      setErroExportacao('Não foi possível gerar a planilha. Tente novamente.')
    } finally {
      setGerandoExcel(false)
    }
  }

  // Exportar PDF: o backend gera o arquivo (QuestPDF) do mesmo período exibido na tela
  const [gerandoPdf, setGerandoPdf] = useState(false)
  const exportarPdf = async () => {
    if (!resumo) return
    setErroExportacao('')
    setGerandoPdf(true)
    try {
      const { arquivo, nomeArquivo } = await baixarPdfRelatorio(resumo.ano, resumo.mes, resumo.meses)
      baixarArquivo(arquivo, nomeArquivo)
    } catch {
      setErroExportacao('Não foi possível gerar o PDF. Tente novamente.')
    } finally {
      setGerandoPdf(false)
    }
  }

  const despesas = resumo?.despesas ?? []
  const comparativo = resumo?.comparativo ?? []
  // Textos e janelas seguem o período dos dados exibidos (o resumo carregado)
  const mesesExibidos = resumo?.meses ?? meses
  const periodoTitulo = mesesExibidos === 1 ? 'do Mês' : 'do Período'
  const mesesGrafico = Math.max(MESES_EVOLUCAO, mesesExibidos)
  const mesesComparativo = Math.max(LIMITE_COMPARATIVO, mesesExibidos)

  return (
    <div className="pagina">
      <div className="pagina-cabecalho">
        <div>
          <h1 className="pagina-titulo">Relatórios Financeiros</h1>
          <p className="pagina-subtitulo">
            Análise detalhada do seu fluxo de caixa, despesas e evolução patrimonial. Considera só
            transações pagas ou recebidas.
          </p>
        </div>
      </div>

      <BarraFiltros
        mes={mes}
        aoMudarMes={setMes}
        meses={meses}
        aoMudarMeses={setMeses}
        aoExportarPdf={resumo && !carregando ? exportarPdf : undefined}
        aoExportarExcel={resumo && !carregando ? exportarExcel : undefined}
        gerandoExcel={gerandoExcel}
        gerandoPdf={gerandoPdf}
      />
      {erroExportacao && <p className="erro">{erroExportacao}</p>}

      {/* Área 1: Resumo executivo */}
      {erro ? (
        <div className="estado-vazio erro">
          <p>{erro}</p>
          <button type="button" className="btn btn-ghost" onClick={tentarNovamente}>
            Tentar novamente
          </button>
        </div>
      ) : carregando || !resumo ? (
        <div className="estado-vazio">Carregando relatório...</div>
      ) : (
        <div className="cards-resumo rel-resumo-grid">
          {montarCards(resumo).map((card) => (
            <ResumoCardFund key={card.rotulo} {...card} />
          ))}
        </div>
      )}

      {/* Área 2: Gráficos (na recarga, mantêm o render anterior esmaecido) */}
      <div className={`rel-graficos-grid ${carregando && resumo ? 'recarregando' : ''}`}>
        <section className="painel">
          <div className="painel-cabecalho">
            <div>
              <div className="painel-titulo">Evolução Temporal</div>
              <div className="painel-sub">Receitas vs. despesas nos últimos {mesesGrafico} meses</div>
            </div>
            <div className="rel-legenda-inline">
              <span className="legend-item"><span className="legend-linha receita" /> Receitas</span>
              <span className="legend-item"><span className="legend-linha despesa" /> Despesas</span>
            </div>
          </div>
          {resumo ? (
            <GraficoEvolucao meses={comparativo} quantidade={mesesGrafico} />
          ) : (
            <div className="estado-vazio">{erro ? 'Gráfico indisponível.' : 'Carregando gráfico...'}</div>
          )}
        </section>

        <section className="painel">
          <div className="painel-cabecalho">
            <div>
              <div className="painel-titulo">Distribuição de Despesas</div>
              <div className="painel-sub">
                Por categoria em {resumo ? rotuloPeriodo(resumo) : 'período selecionado'}
              </div>
            </div>
          </div>
          {resumo ? (
            <GraficoDistribuicao despesas={despesas} />
          ) : (
            <div className="estado-vazio">{erro ? 'Gráfico indisponível.' : 'Carregando gráfico...'}</div>
          )}
        </section>
      </div>

      {/* Área 3: Tabelas de performance */}
      <div className="rel-tabelas-grid">
        <section className="painel">
          <div className="painel-cabecalho">
            <div>
              <div className="painel-titulo">
                {despesasExpandidas
                  ? `Todas as Despesas ${periodoTitulo}`
                  : `Top 5 Maiores Despesas ${periodoTitulo}`}
              </div>
              <div className="painel-sub">
                {despesasExpandidas
                  ? 'Da maior para a menor no período selecionado'
                  : 'Maiores saídas no período selecionado'}
              </div>
            </div>
            {!carregando && despesas.length > 0 && (
              <BotaoVerTodas
                expandida={despesasExpandidas}
                rotulo={`Ver todas (${despesas.length})`}
                aoAlternar={() => setDespesasExpandidas((v) => !v)}
              />
            )}
          </div>
          <TabelaTopDespesas itens={despesas} expandida={despesasExpandidas} carregando={carregando} />
        </section>

        <section className="painel">
          <div className="painel-cabecalho">
            <div>
              <div className="painel-titulo">Comparativo Mensal</div>
              <div className="painel-sub">
                {comparativoExpandido
                  ? 'Desde a primeira transação — receitas, despesas e resultado'
                  : `Últimos ${mesesComparativo} meses — receitas, despesas e resultado`}
              </div>
            </div>
            {!carregando && comparativo.length > 0 && (
              <BotaoVerTodas
                expandida={comparativoExpandido}
                rotulo={`Ver todos (${comparativo.length} meses)`}
                aoAlternar={() => setComparativoExpandido((v) => !v)}
              />
            )}
          </div>
          <TabelaComparativo
            linhas={comparativo}
            expandida={comparativoExpandido}
            carregando={carregando}
            limite={mesesComparativo}
          />
        </section>
      </div>
    </div>
  )
}
