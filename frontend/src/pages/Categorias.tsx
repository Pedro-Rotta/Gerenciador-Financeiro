/**
 * Categorias — Seção de Categorias de Despesas e Receitas.
 * Lista expansível (accordion) com subcategorias, barras de progresso
 * de orçamento e totais mensais. Carrega os dados do backend e permite
 * criar novas categorias e subcategorias via POST sincronizado.
 */

import { useState, useEffect, useCallback, useMemo, useRef, type FormEvent, type ReactNode } from 'react'
import {
  IconeTalheres,
  IconeCarro,
  IconeCasa,
  IconeCartas,
  IconeDolar,
  IconeGrafico,
  IconeEllipsisVertical,
  IconeSetaCategoria,
  IconeMais,
  IconeLapis,
  IconeLixeira,
} from '../components/icons'
import Modal from '../components/Modal'
import OpcoesDeConta from '../components/OpcoesDeConta'
import FiltrosTransacoes from '../components/FiltrosTransacoes'
import { intervaloDoPeriodo, type Filtros, type OpcaoCategoria } from '../utils/filtrosTransacoes'
import {
  carregarOpcoesConta,
  filtroDaConta,
  pertenceAConta,
  type OpcaoConta,
} from '../services/opcoesConta'
import {
  criarCategoria,
  listarCategoriasPorConta,
  listarCategoriasPorContaConjunta,
  editarCategoria,
  excluirCategoria,
} from '../services/categorias'
import {
  criarSubCategoria,
  listarSubCategoriasPorConta,
  listarSubCategoriasPorContaConjunta,
  vincularSubCategoria,
  desvincularSubCategoria,
  editarSubCategoria,
  excluirSubCategoria,
} from '../services/subcategorias'
import { listarTransacoes } from '../services/transacoes'
import { TipoCategoria as TipoCategoriaEnum } from '../types/categoria'
import type { CategoriaResponse } from '../types/categoria'
import type { SubCategoriaResponse } from '../types/subcategoria'
import type { TransacaoResponse } from '../types/transacao'
import type { TipoCategoria as TipoCategoriaBackend } from '../types/categoria'
import { extrairErro } from '../utils/erros'
/* ---------- Tipos ---------- */

type TipoCategoria = 'despesa' | 'receita'

interface Subcategoria {
  id: number
  nome: string
  valor: number
}

interface Categoria {
  id: number
  nome: string
  icone: ReactNode
  cor: 'laranja' | 'azul' | 'verde' | 'roxo' | 'rosa' | 'ambar'
  quantidadeTransacoes: number
  valorNoMes: number
  orcamentoLimite?: number
  subcategorias: Subcategoria[]
  tipo: TipoCategoria
  /** Se a despesa é necessária (vem do backend; usado ao editar). */
  necessario: boolean
  /** Conta à qual a categoria pertence (para vincular subcategorias). */
  contaId: number | null
  contaConjuntaId: number | null
}

/* ---------- Helpers ---------- */

function moeda(valor: number) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function total(lista: Categoria[]) {
  return lista.reduce((acc, c) => acc + c.valorNoMes, 0)
}

// Ordena alfabeticamente por nome (ignorando acentos/caixa)
function porNome(a: Categoria, b: Categoria) {
  return a.nome.localeCompare(b.nome, 'pt-BR')
}

/* ---------- Conversão para o modelo de UI (exibição) ---------- */

const PALETAS: Record<Categoria['cor'], { icone: ReactNode; cor: Categoria['cor'] }> = {
  laranja: { icone: <IconeTalheres />, cor: 'laranja' },
  azul: { icone: <IconeCarro />, cor: 'azul' },
  verde: { icone: <IconeCasa />, cor: 'verde' },
  roxo: { icone: <IconeDolar />, cor: 'roxo' },
  rosa: { icone: <IconeGrafico />, cor: 'rosa' },
  ambar: { icone: <IconeCartas />, cor: 'ambar' },
}

// Derivar ícone/cor a partir de um nome (estável por hash simples do nome)
function paletaParaNome(nome: string): Categoria['cor'] {
  const cores: Categoria['cor'][] = ['laranja', 'azul', 'verde', 'roxo', 'rosa', 'ambar']
  const soma = nome.split('').reduce((acc, c) => acc + c.charCodeAt(0), 0)
  return cores[soma % cores.length]
}

// Converte uma CategoriaResponse do backend para o modelo de UI de exibição
function responseParaUi(resposta: CategoriaResponse): Categoria {
  const cor = paletaParaNome(resposta.nome)
  return {
    id: resposta.id,
    nome: resposta.nome,
    icone: PALETAS[cor].icone,
    cor,
    quantidadeTransacoes: 0,
    valorNoMes: 0,
    subcategorias: (resposta.subCategorias ?? [])
      .map((s) => ({
        id: s.id,
        nome: s.nome,
        valor: 0,
      }))
      .sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')),
    tipo: resposta.tipo === TipoCategoriaEnum.ENTRADA ? 'receita' : 'despesa',
    necessario: resposta.necessario,
    contaId: resposta.contaId,
    contaConjuntaId: resposta.contaConjuntaId,
  }
}

/* ---------- Agregação de transações por categoria (período do filtro) ---------- */

interface AgregadoCategoria {
  valor: number
  quantidade: number
  subcategorias: Record<number, number>
}

/**
 * Agrega as transações do período por categoria/subcategoria.
 * Retorna um mapa categoriaId -> { valor, quantidade, subcategorias }.
 * Só soma transações que pertencem à conta informada (individual ou conjunta).
 */
function agregarTransacoesPorCategoria(
  transacoes: TransacaoResponse[],
  conta: OpcaoConta,
  periodo: { de: string | null; ate: string | null },
): Map<number, AgregadoCategoria> {
  const mapa = new Map<number, AgregadoCategoria>()

  for (const t of transacoes) {
    // Apenas transações da conta e do período
    if (!pertenceAConta(t, conta)) continue
    if (periodo.de && t.dataTransacao < periodo.de) continue
    if (periodo.ate && t.dataTransacao > periodo.ate) continue
    if (t.categoriaId == null) continue

    let agregado = mapa.get(t.categoriaId)
    if (!agregado) {
      agregado = { valor: 0, quantidade: 0, subcategorias: {} }
      mapa.set(t.categoriaId, agregado)
    }

    agregado.valor += t.valor
    agregado.quantidade += 1

    if (t.subCategoriaId != null) {
      agregado.subcategorias[t.subCategoriaId] =
        (agregado.subcategorias[t.subCategoriaId] ?? 0) + t.valor
    }
  }

  return mapa
}

/* ---------- Busca por conta (individual ou conjunta) ---------- */

function listarCategoriasDaConta(conta: OpcaoConta) {
  return conta.conjunta
    ? listarCategoriasPorContaConjunta(conta.id)
    : listarCategoriasPorConta(conta.id)
}

function listarSubCategoriasDaConta(conta: OpcaoConta) {
  return conta.conjunta
    ? listarSubCategoriasPorContaConjunta(conta.id)
    : listarSubCategoriasPorConta(conta.id)
}

// Aplica os totais agregados (período do filtro) ao modelo de UI de uma categoria
function aplicarTotais(categoria: Categoria, agregado?: AgregadoCategoria): Categoria {
  if (!agregado) {
    return {
      ...categoria,
      valorNoMes: 0,
      quantidadeTransacoes: 0,
      subcategorias: categoria.subcategorias.map((s) => ({ ...s, valor: 0 })),
    }
  }
  return {
    ...categoria,
    valorNoMes: agregado.valor,
    quantidadeTransacoes: agregado.quantidade,
    subcategorias: categoria.subcategorias.map((s) => ({
      ...s,
      valor: agregado.subcategorias[s.id] ?? 0,
    })),
  }
}

/* ---------- Erros ---------- */

/* ---------- Menu de três pontos ---------- */

interface MenuOpcoesProps {
  onEditar?: () => void
  onAdicionar?: () => void
  onExcluir?: () => void
}

function MenuOpcoes({ onEditar, onAdicionar, onExcluir }: MenuOpcoesProps) {
  const [aberto, setAberto] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  // Fecha o menu ao clicar fora dele
  useEffect(() => {
    if (!aberto) return
    const aoClicarFora = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setAberto(false)
      }
    }
    document.addEventListener('mousedown', aoClicarFora)
    return () => document.removeEventListener('mousedown', aoClicarFora)
  }, [aberto])

  return (
    <div className="cat-menu" ref={containerRef}>
      <button
        type="button"
        className="cat-menu-btn"
        aria-label="Mais opções"
        onClick={(e) => {
          e.stopPropagation()
          setAberto((v) => !v)
        }}
      >
        <IconeEllipsisVertical />
      </button>
      {aberto && (
        <div className="cat-menu-dropdown">
          <button type="button" className="cat-menu-item" onClick={() => { setAberto(false); onEditar?.() }}>
            Editar
          </button>
          <button type="button" className="cat-menu-item" onClick={() => { setAberto(false); onAdicionar?.() }}>
            Adicionar Subcategoria
          </button>
          <button type="button" className="cat-menu-item excluir" onClick={() => { setAberto(false); onExcluir?.() }}>
            Excluir
          </button>
        </div>
      )}
    </div>
  )
}

/* ---------- Item de categoria (accordion) ---------- */

interface ItemCategoriaProps {
  categoria: Categoria
  onEditar: (categoria: Categoria) => void
  onAdicionarSubcategoria: (categoria: Categoria) => void
  onExcluir: (categoria: Categoria) => void
  onDesvincularSubcategoria: (categoria: Categoria, subId: number) => Promise<void>
}

function ItemCategoria({ categoria, onEditar, onAdicionarSubcategoria, onExcluir, onDesvincularSubcategoria }: ItemCategoriaProps) {
  const [aberto, setAberto] = useState(false)
  const [desvinculandoId, setDesvinculandoId] = useState<number | null>(null)
  const { nome, icone, cor, quantidadeTransacoes, valorNoMes, orcamentoLimite, subcategorias } = categoria

  const temOrcamento = typeof orcamentoLimite === 'number' && orcamentoLimite > 0
  const usoPct = temOrcamento ? Math.min((valorNoMes / (orcamentoLimite as number)) * 100, 100) : null

  return (
    <div className={`cat-item ${aberto ? 'aberto' : ''}`}>
      <div
        className="cat-item-head"
        role="button"
        tabIndex={0}
        onClick={() => setAberto((v) => !v)}
        onKeyDown={(e) => e.key === 'Enter' && setAberto((v) => !v)}
      >
        <div className="cat-item-icone"><span className={`cor-${cor}`}>{icone}</span></div>

        <div className="cat-item-info">
          <div className="cat-item-nome">{nome}</div>
          <div className="cat-item-meta">{quantidadeTransacoes} transações</div>

          {usoPct !== null && (
            <div className="cat-bar">
              <div
                className={`cat-bar-fill ${usoPct > 85 ? 'alto' : ''}`}
                style={{ width: `${usoPct}%` }}
              />
            </div>
          )}
        </div>

        <div className="cat-item-acao">
          <span className={`cat-item-valor ${categoria.tipo === 'receita' ? 'receita' : ''}`}>
            {moeda(valorNoMes)}
          </span>
          <span className="cat-item-chevron">
            <IconeSetaCategoria />
          </span>
          <MenuOpcoes
            onEditar={() => onEditar(categoria)}
            onAdicionar={() => onAdicionarSubcategoria(categoria)}
            onExcluir={() => onExcluir(categoria)}
          />
        </div>
      </div>

      {aberto && subcategorias.length > 0 && (
        <div className="cat-sub">
          {subcategorias.map((sub) => (
            <div key={sub.id ?? sub.nome} className="cat-sub-item">
              <span className="cat-sub-nome">{sub.nome}</span>
              <span className={`cat-sub-valor ${categoria.tipo === 'receita' ? 'receita' : ''}`}>
                {moeda(sub.valor)}
              </span>
              <button
                type="button"
                className="cat-sub-desvincular"
                aria-label={`Desvincular ${sub.nome}`}
                title="Desvincular subcategoria"
                disabled={desvinculandoId === sub.id}
                onClick={async (e) => {
                  e.stopPropagation()
                  setDesvinculandoId(sub.id)
                  try {
                    await onDesvincularSubcategoria(categoria, sub.id)
                  } finally {
                    setDesvinculandoId(null)
                  }
                }}
              >
                <IconeLixeira />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

/* ---------- Coluna (bloco de despesas ou receitas) ---------- */

interface ColunaCategoriasProps {
  titulo: string
  lista: Categoria[]
  onEditar: (categoria: Categoria) => void
  onAdicionarSubcategoria: (categoria: Categoria) => void
  onExcluir: (categoria: Categoria) => void
  onDesvincularSubcategoria: (categoria: Categoria, subId: number) => Promise<void>
}

function ColunaCategorias({ titulo, lista, onEditar, onAdicionarSubcategoria, onExcluir, onDesvincularSubcategoria }: ColunaCategoriasProps) {
  const totalNoMes = total(lista)
  const isReceita = titulo === 'Receitas'

  return (
    <section className="painel cat-coluna">
      <div className="cat-coluna-cabecalho">
        <h2>{titulo}</h2>
        <span className={`cat-coluna-total ${isReceita ? 'verde' : ''}`}>{moeda(totalNoMes)}</span>
      </div>

      <div className="cat-lista">
        {lista.map((cat) => (
          <ItemCategoria
            key={cat.id}
            categoria={cat}
            onEditar={onEditar}
            onAdicionarSubcategoria={onAdicionarSubcategoria}
            onExcluir={onExcluir}
            onDesvincularSubcategoria={onDesvincularSubcategoria}
          />
        ))}
      </div>
    </section>
  )
}

/* ---------- Página principal ---------- */

/** Filtros iniciais: totais do mês atual (mesmo comportamento de antes), tudo visível */
const FILTROS_PADRAO: Filtros = {
  tipo: 'todas',
  periodo: 'mes',
  de: '',
  ate: '',
  conta: '',
  categoria: '',
}

export default function Categorias() {
  // Categorias da conta ativa (sem totais: são aplicados conforme o período do filtro)
  const [despesas, setDespesas] = useState<Categoria[]>([])
  const [receitas, setReceitas] = useState<Categoria[]>([])
  const [transacoesConta, setTransacoesConta] = useState<TransacaoResponse[]>([])

  // Contas individuais + conjuntas; a ativa é identificada pela chave ("c:ID" ou "cc:ID")
  const [contas, setContas] = useState<OpcaoConta[]>([])
  const [contaAtivaChave, setContaAtivaChave] = useState('')

  // Filtros (a conta do filtro é sempre a conta ativa)
  const [filtros, setFiltros] = useState<Filtros>(FILTROS_PADRAO)

  const [carregando, setCarregando] = useState(true)
  const [erroBusca, setErroBusca] = useState('')

  const [modalCategoria, setModalCategoria] = useState(false)
  const [modalGerenciarSubcategorias, setModalGerenciarSubcategorias] = useState(false)

  // Modais do menu de três pontos
  const [categoriaEditar, setCategoriaEditar] = useState<Categoria | null>(null)
  const [categoriaVincular, setCategoriaVincular] = useState<Categoria | null>(null)
  const [categoriaExcluir, setCategoriaExcluir] = useState<Categoria | null>(null)

  const carregarCategoriasDaConta = useCallback(async (conta: OpcaoConta) => {
    setCarregando(true)
    setErroBusca('')
    try {
      const [categorias, transacoes] = await Promise.all([
        listarCategoriasDaConta(conta),
        listarTransacoes(filtroDaConta(conta)),
      ])
      const uis = categorias.map(responseParaUi)
      setTransacoesConta(transacoes)
      setDespesas(uis.filter((c) => c.tipo === 'despesa').sort(porNome))
      setReceitas(uis.filter((c) => c.tipo === 'receita').sort(porNome))
    } catch (err: unknown) {
      setErroBusca(extrairErro(err, 'Não foi possível carregar as categorias.'))
    } finally {
      setCarregando(false)
    }
  }, [])

  const carregarCategorias = useCallback(async () => {
    setCarregando(true)
    setErroBusca('')
    try {
      const contasData = await carregarOpcoesConta()
      // Define a conta ativa: por padrão a conta principal, se existir.
      const ativa = contasData.find((c) => c.principal) ?? contasData[0]
      setContas(contasData)
      setContaAtivaChave(ativa?.chave ?? '')

      if (!ativa) {
        setDespesas([])
        setReceitas([])
        setCarregando(false)
        return
      }

      await carregarCategoriasDaConta(ativa)
    } catch (err: unknown) {
      setErroBusca(extrairErro(err, 'Não foi possível carregar as categorias.'))
      setCarregando(false)
    }
  }, [carregarCategoriasDaConta])

  useEffect(() => {
    carregarCategorias()
  }, [carregarCategorias])

  const selecionarConta = async (conta: OpcaoConta) => {
    setContaAtivaChave(conta.chave)
    await carregarCategoriasDaConta(conta)
  }

  const contaAtiva = contas.find((c) => c.chave === contaAtivaChave)

  // Troca de conta pelo filtro recarrega as categorias; a categoria escolhida
  // é descartada se não existir na nova conta
  const aoMudarFiltros = (novos: Filtros) => {
    const novaConta = contas.find((c) => c.chave === novos.conta)
    if (novaConta && novaConta.chave !== contaAtivaChave) {
      setFiltros({ ...novos, categoria: '' })
      void selecionarConta(novaConta)
      return
    }
    setFiltros(novos)
  }

  // Totais do período e filtros de tipo/categoria aplicados às listas exibidas
  const { despesasVisiveis, receitasVisiveis } = useMemo(() => {
    const agregados = contaAtiva
      ? agregarTransacoesPorCategoria(transacoesConta, contaAtiva, intervaloDoPeriodo(filtros))
      : new Map<number, AgregadoCategoria>()
    const preparar = (lista: Categoria[]) =>
      lista
        .filter((c) => !filtros.categoria || c.nome === filtros.categoria)
        .map((c) => aplicarTotais(c, agregados.get(c.id)))
    return {
      despesasVisiveis: filtros.tipo === 'entradas' ? [] : preparar(despesas),
      receitasVisiveis: filtros.tipo === 'saidas' ? [] : preparar(receitas),
    }
  }, [contaAtiva, transacoesConta, filtros, despesas, receitas])

  const opcoesCategoria: OpcaoCategoria[] = [
    ...despesas.map((c): OpcaoCategoria => ({ nome: c.nome, tipo: 'saida' })),
    ...receitas.map((c): OpcaoCategoria => ({ nome: c.nome, tipo: 'entrada' })),
  ]

  const adicionarCategoria = (cat: Categoria) => {
    // Só entra na lista se foi criada na conta que está sendo exibida
    if (!contaAtiva || !pertenceAConta(cat, contaAtiva)) return
    setDespesas((prev) => (cat.tipo === 'despesa' ? [...prev, cat].sort(porNome) : prev))
    setReceitas((prev) => (cat.tipo === 'receita' ? [...prev, cat].sort(porNome) : prev))
  }

  // Atualiza uma categoria na lista (edição ou vínculo de subcategoria).
  // Se o tipo mudou (despesa <-> receita), a categoria é movida entre colunas.
  const atualizarCategoria = (atualizada: Categoria) => {
    setDespesas((prev) => {
      const sem = prev.filter((c) => c.id !== atualizada.id)
      return atualizada.tipo === 'despesa' ? [...sem, atualizada].sort(porNome) : sem
    })
    setReceitas((prev) => {
      const sem = prev.filter((c) => c.id !== atualizada.id)
      return atualizada.tipo === 'receita' ? [...sem, atualizada].sort(porNome) : sem
    })
  }

  // Remove uma categoria da lista (exclusão)
  const removerCategoria = (id: number) => {
    setDespesas((prev) => prev.filter((c) => c.id !== id))
    setReceitas((prev) => prev.filter((c) => c.id !== id))
  }

  // Desvincula uma subcategoria de uma categoria (POST desvincular) e atualiza a lista
  const desvincularSubcategoriaLocal = async (categoria: Categoria, subId: number) => {
    await desvincularSubCategoria({ idCategoria: categoria.id, idSubCategoria: subId })
    const remover = (lista: Categoria[]) =>
      lista.map((c) =>
        c.id === categoria.id
          ? { ...c, subcategorias: c.subcategorias.filter((s) => s.id !== subId) }
          : c,
      )
    setDespesas((prev) => remover(prev))
    setReceitas((prev) => remover(prev))
  }

  return (
    <div className="pagina">
      <div className="pagina-cabecalho">
        <div>
          <h1 className="pagina-titulo">Categorias</h1>
          <p className="pagina-subtitulo">Gerencie e organize suas categorias de receitas e despesas.</p>
        </div>
        <div className="pagina-cabecalho-acoes">
          <button type="button" className="btn btn-primario" onClick={() => setModalCategoria(true)}>
            <IconeMais />
            <span className="btn-label">Nova Categoria</span>
          </button>
          <button type="button" className="btn btn-primario" onClick={() => setModalGerenciarSubcategorias(true)}>
            <IconeLapis />
            <span className="btn-label">Subcategorias</span>
          </button>
        </div>
      </div>

      {contaAtiva && (
        <div className="rel-filtros cat-filtros">
          <div className="cat-filtros-titulo">
            Mostrando categorias de <strong>{contaAtiva.nome}</strong>
          </div>
          <FiltrosTransacoes
            valor={{ ...filtros, conta: contaAtivaChave }}
            aoMudar={aoMudarFiltros}
            padrao={{ ...FILTROS_PADRAO, conta: contaAtivaChave }}
            contas={contas}
            categorias={opcoesCategoria}
            permitirTodasContas={false}
          />
        </div>
      )}

      {carregando ? (
        <div className="estado-vazio">Carregando categorias...</div>
      ) : erroBusca ? (
        <div className="estado-vazio erro">
          <p>{erroBusca}</p>
          <button type="button" className="btn btn-ghost" onClick={carregarCategorias}>
            Tentar novamente
          </button>
        </div>
      ) : (
        <div className={`cat-grid ${filtros.tipo !== 'todas' ? 'unica' : ''}`}>
          {filtros.tipo !== 'entradas' && (
            <ColunaCategorias
              titulo="Despesas"
              lista={despesasVisiveis}
              onEditar={setCategoriaEditar}
              onAdicionarSubcategoria={setCategoriaVincular}
              onExcluir={setCategoriaExcluir}
              onDesvincularSubcategoria={desvincularSubcategoriaLocal}
            />
          )}
          {filtros.tipo !== 'saidas' && (
            <ColunaCategorias
              titulo="Receitas"
              lista={receitasVisiveis}
              onEditar={setCategoriaEditar}
              onAdicionarSubcategoria={setCategoriaVincular}
              onExcluir={setCategoriaExcluir}
              onDesvincularSubcategoria={desvincularSubcategoriaLocal}
            />
          )}
        </div>
      )}

      <ModalCategoria
        aberto={modalCategoria}
        aoFechar={() => setModalCategoria(false)}
        aoCriar={adicionarCategoria}
        contaChave={contaAtivaChave}
      />

      <ModalEditarCategoria
        categoria={categoriaEditar}
        aoFechar={() => setCategoriaEditar(null)}
        aoSalvar={atualizarCategoria}
      />
      <ModalAdicionarSubcategoria
        categoria={categoriaVincular}
        aoFechar={() => setCategoriaVincular(null)}
        aoVincular={atualizarCategoria}
      />
      <ModalExcluirCategoria
        categoria={categoriaExcluir}
        aoFechar={() => setCategoriaExcluir(null)}
        aoExcluir={removerCategoria}
      />

      <ModalGerenciarSubcategorias
        aberto={modalGerenciarSubcategorias}
        aoFechar={() => setModalGerenciarSubcategorias(false)}
        contaChave={contaAtivaChave}
      />
    </div>
  )
}

/* ---------- Modal de criação de categoria ---------- */

interface ModalCategoriaProps {
  aberto: boolean
  aoFechar: () => void
  aoCriar: (cat: Categoria) => void
  /** Conta exibida na página ("c:ID" ou "cc:ID"): vem pré-selecionada. */
  contaChave: string
}

function ModalCategoria({ aberto, aoFechar, aoCriar, contaChave }: ModalCategoriaProps) {
  const [contas, setContas] = useState<OpcaoConta[]>([])
  const [carregandoContas, setCarregandoContas] = useState(false)

  const [nome, setNome] = useState('')
  const [tipo, setTipo] = useState<TipoCategoriaBackend>(TipoCategoriaEnum.SAIDA)
  const [necessario, setNecessario] = useState(true)
  const [contaSelecionadaChave, setContaSelecionadaChave] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  // Ao abrir, busca as contas (individuais e conjuntas) e pré-seleciona a da página
  useEffect(() => {
    if (!aberto) return
    setErro('')
    setCarregandoContas(true)
    carregarOpcoesConta()
      .then((data) => {
        setContas(data)
        const inicial = data.find((c) => c.chave === contaChave) ?? data[0]
        setContaSelecionadaChave(inicial?.chave ?? '')
      })
      .catch(() => setErro('Não foi possível carregar suas contas.'))
      .finally(() => setCarregandoContas(false))
  }, [aberto, contaChave])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErro('')
    const conta = contas.find((c) => c.chave === contaSelecionadaChave)
    if (!conta) {
      setErro('Selecione uma conta para vincular.')
      return
    }
    setCarregando(true)
    try {
      const criada = await criarCategoria({
        nome: nome.trim(),
        tipo,
        // "Necessária" só se aplica a despesas; para receita envia false.
        necessario: tipo === TipoCategoriaEnum.SAIDA && necessario,
        icone: null,
        // Exatamente uma: conta individual OU conta conjunta
        ...filtroDaConta(conta),
      })
      // Usa a resposta real do backend (preserva contaId/contaConjuntaId) para
      // que a categoria recém-criada já permita vincular subcategorias.
      aoCriar(responseParaUi(criada))
      aoFechar()
      setNome('')
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível criar a categoria.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Nova Categoria"
      subtitulo="Crie uma nova categoria para organizar suas finanças."
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Nome
          <input
            type="text"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            placeholder="Ex: Lazer, Saúde, Moradia..."
            minLength={2}
            maxLength={100}
            required
            autoFocus
          />
        </label>

        <label className="campo">
          Tipo
          <select
            value={tipo}
            onChange={(e) => setTipo(Number(e.target.value) as TipoCategoriaBackend)}
          >
            <option value={TipoCategoriaEnum.SAIDA}>Despesa</option>
            <option value={TipoCategoriaEnum.ENTRADA}>Receita</option>
          </select>
        </label>

        {tipo === TipoCategoriaEnum.SAIDA && (
          <label className="campo-check">
            <input
              type="checkbox"
              checked={necessario}
              onChange={(e) => setNecessario(e.target.checked)}
            />
            <span>É uma despesa necessária</span>
          </label>
        )}

        <label className="campo">
          Conta
          <select
            value={contaSelecionadaChave}
            onChange={(e) => setContaSelecionadaChave(e.target.value)}
            disabled={carregandoContas}
            required
          >
            {carregandoContas && <option value="">Carregando contas...</option>}
            {!carregandoContas && contas.length === 0 && (
              <option value="">Nenhuma conta encontrada</option>
            )}
            {!carregandoContas && <OpcoesDeConta opcoes={contas} />}
          </select>
        </label>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primario" disabled={carregando}>
            {carregando ? 'Criando...' : 'Criar Categoria'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

/* ---------- Modal de edição de categoria ---------- */

interface ModalEditarCategoriaProps {
  categoria: Categoria | null
  aoFechar: () => void
  aoSalvar: (atualizada: Categoria) => void
}

function ModalEditarCategoria({ categoria, aoFechar, aoSalvar }: ModalEditarCategoriaProps) {
  const [nome, setNome] = useState('')
  const [tipo, setTipo] = useState<TipoCategoriaBackend>(TipoCategoriaEnum.SAIDA)
  const [necessario, setNecessario] = useState(true)
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const aberto = categoria !== null

  // Preenche o formulário sempre que a categoria alvo mudar
  useEffect(() => {
    if (!categoria) return
    setNome(categoria.nome)
    setTipo(categoria.tipo === 'receita' ? TipoCategoriaEnum.ENTRADA : TipoCategoriaEnum.SAIDA)
    setNecessario(categoria.necessario)
    setErro('')
  }, [categoria])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!categoria) return
    setErro('')
    setCarregando(true)
    try {
      const atualizada = await editarCategoria(categoria.id, {
        nome: nome.trim(),
        tipo,
        // "Necessária" só se aplica a despesas; para receita envia false.
        necessario: tipo === TipoCategoriaEnum.SAIDA && necessario,
        icone: null,
      })
      const ui = responseParaUi(atualizada)
      // Mantém os valores de exibição da categoria atual (transações/valor e
      // subcategorias, que a edição não altera nem devolve) e aplica
      // nome/tipo/necessário/contas recém-salvos do backend.
      aoSalvar({
        ...categoria,
        nome: ui.nome,
        tipo: ui.tipo,
        necessario: ui.necessario,
        contaId: ui.contaId,
        contaConjuntaId: ui.contaConjuntaId,
      })
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível editar a categoria.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Editar Categoria"
      subtitulo="Atualize as informações da categoria."
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Nome
          <input
            type="text"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            placeholder="Ex: Lazer, Saúde, Moradia..."
            minLength={2}
            maxLength={100}
            required
            autoFocus
          />
        </label>

        <label className="campo">
          Tipo
          <select
            value={tipo}
            onChange={(e) => setTipo(Number(e.target.value) as TipoCategoriaBackend)}
          >
            <option value={TipoCategoriaEnum.SAIDA}>Despesa</option>
            <option value={TipoCategoriaEnum.ENTRADA}>Receita</option>
          </select>
        </label>

        {tipo === TipoCategoriaEnum.SAIDA && (
          <label className="campo-check">
            <input
              type="checkbox"
              checked={necessario}
              onChange={(e) => setNecessario(e.target.checked)}
            />
            <span>É uma despesa necessária</span>
          </label>
        )}

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

/* ---------- Modal de adicionar subcategoria (vincular) ---------- */

interface ModalAdicionarSubcategoriaProps {
  categoria: Categoria | null
  aoFechar: () => void
  aoVincular: (atualizada: Categoria) => void
}

function ModalAdicionarSubcategoria({
  categoria,
  aoFechar,
  aoVincular,
}: ModalAdicionarSubcategoriaProps) {
  const [subcategorias, setSubcategorias] = useState<SubCategoriaResponse[]>([])
  const [carregandoLista, setCarregandoLista] = useState(false)
  const [subcategoriaId, setSubcategoriaId] = useState<number>(0)
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const aberto = categoria !== null

  // Ao abrir, lista as subcategorias da conta da categoria selecionada (individual ou conjunta)
  useEffect(() => {
    if (!categoria) return
    setErro('')
    setSubcategoriaId(0)
    setSubcategorias([])

    const busca =
      categoria.contaId != null
        ? listarSubCategoriasPorConta(categoria.contaId)
        : categoria.contaConjuntaId != null
          ? listarSubCategoriasPorContaConjunta(categoria.contaConjuntaId)
          : null
    if (!busca) {
      setErro('Essa categoria não está vinculada a nenhuma conta.')
      setCarregandoLista(false)
      return
    }

    setCarregandoLista(true)
    busca
      .then((data) => {
        setSubcategorias(data)
        if (data.length > 0) setSubcategoriaId(data[0].id)
      })
      .catch(() => setErro('Não foi possível carregar as subcategorias.'))
      .finally(() => setCarregandoLista(false))
  }, [categoria])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!categoria) return
    setErro('')
    if (!subcategoriaId) {
      setErro('Selecione uma subcategoria para vincular.')
      return
    }
    setCarregando(true)
    try {
      const atualizada = await vincularSubCategoria({
        idCategoria: categoria.id,
        idSubCategoria: subcategoriaId,
      })
      const ui = responseParaUi(atualizada)
      aoVincular({ ...ui, ...categoria, subcategorias: ui.subcategorias })
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível vincular a subcategoria.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Adicionar Subcategoria"
      subtitulo={categoria ? `Vincule a um detalhe de gasto em "${categoria.nome}".` : undefined}
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Subcategoria
          <select
            value={subcategoriaId}
            onChange={(e) => setSubcategoriaId(Number(e.target.value))}
            disabled={carregandoLista}
            required
          >
            {carregandoLista && <option value={0}>Carregando subcategorias...</option>}
            {!carregandoLista && subcategorias.length === 0 && (
              <option value={0}>Nenhuma subcategoria disponível</option>
            )}
            {subcategorias.map((s) => (
              <option key={s.id} value={s.id}>
                {s.nome}
              </option>
            ))}
          </select>
        </label>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primario" disabled={carregando || carregandoLista}>
            {carregando ? 'Vinculando...' : 'Vincular'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

/* ---------- Modal de exclusão de categoria ---------- */

interface ModalExcluirCategoriaProps {
  categoria: Categoria | null
  aoFechar: () => void
  aoExcluir: (id: number) => void
}

function ModalExcluirCategoria({ categoria, aoFechar, aoExcluir }: ModalExcluirCategoriaProps) {
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const aberto = categoria !== null

  useEffect(() => {
    if (categoria) setErro('')
  }, [categoria])

  const handleConfirmar = async () => {
    if (!categoria) return
    setErro('')
    setCarregando(true)
    try {
      await excluirCategoria(categoria.id)
      aoExcluir(categoria.id)
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível excluir a categoria.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Excluir Categoria"
      subtitulo="Esta ação não pode ser desfeita."
      aoFechar={aoFechar}
    >
      <div className="formulario">
        <p>
          Tem certeza que deseja excluir a categoria{' '}
          <strong>{categoria?.nome}</strong>?
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

/* ---------- Modal de gerenciar subcategorias (GET/POST/PUT/DELETE) ---------- */

interface ModalGerenciarSubcategoriasProps {
  aberto: boolean
  aoFechar: () => void
  /** Conta acessada na página ("c:ID" ou "cc:ID"): o modal abre mostrando só as subcategorias dela. */
  contaChave: string
}

function ModalGerenciarSubcategorias({
  aberto,
  aoFechar,
  contaChave,
}: ModalGerenciarSubcategoriasProps) {
  // GET — subcategorias da conta selecionada
  const [subcategorias, setSubcategorias] = useState<SubCategoriaResponse[]>([])
  const [carregando, setCarregando] = useState(false)
  const [erro, setErro] = useState('')
  const [recarga, setRecarga] = useState(0)

  // Conta exibida na lista e usada na criação (começa na conta acessada; individual ou conjunta)
  const [contas, setContas] = useState<OpcaoConta[]>([])
  const [contaSelecionadaChave, setContaSelecionadaChave] = useState('')
  const contaSelecionada = contas.find((c) => c.chave === contaSelecionadaChave)

  // Formulário de criação (POST)
  const [novoNome, setNovoNome] = useState('')
  const [criando, setCriando] = useState(false)

  // Edição inline (PUT)
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [editandoNome, setEditandoNome] = useState('')
  const [editandoBtn, setEditandoBtn] = useState(false)

  // Exclusão (DELETE) com confirmação
  const [excluindoId, setExcluindoId] = useState<number | null>(null)
  const [excluindoBtn, setExcluindoBtn] = useState(false)

  // Ao abrir: começa na conta acessada na página e carrega as contas do seletor
  useEffect(() => {
    if (!aberto) return
    setEditandoId(null)
    setExcluindoId(null)
    setNovoNome('')
    setErro('')
    setContaSelecionadaChave(contaChave)
    carregarOpcoesConta()
      .then((todasContas) => {
        setContas(todasContas)
        // Sem conta acessada válida: usa a primeira conta do membro
        if (!todasContas.some((c) => c.chave === contaChave)) {
          setContaSelecionadaChave(todasContas[0]?.chave ?? '')
        }
      })
      .catch((err: unknown) => setErro(extrairErro(err, 'Não foi possível carregar as contas.')))
  }, [aberto, contaChave])

  // GET — lista apenas as subcategorias da conta selecionada (recarrega ao trocar de conta)
  useEffect(() => {
    if (!aberto || !contaSelecionada) return
    let cancelado = false
    setCarregando(true)
    setErro('')
    setEditandoId(null)
    setExcluindoId(null)
    listarSubCategoriasDaConta(contaSelecionada)
      .then((subs) => {
        if (!cancelado) setSubcategorias([...subs].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')))
      })
      .catch((err: unknown) => {
        if (!cancelado) setErro(extrairErro(err, 'Não foi possível carregar as subcategorias.'))
      })
      .finally(() => {
        if (!cancelado) setCarregando(false)
      })
    return () => {
      cancelado = true
    }
  }, [aberto, contaSelecionada, recarga])

  // POST — cria uma nova subcategoria na conta selecionada
  const handleCriar = async (e: FormEvent) => {
    e.preventDefault()
    setErro('')
    if (!contaSelecionada) {
      setErro('Selecione uma conta para a nova subcategoria.')
      return
    }
    setCriando(true)
    try {
      const criada = await criarSubCategoria({ nome: novoNome.trim(), ...filtroDaConta(contaSelecionada) })
      setSubcategorias((prev) =>
        [...prev, criada].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')),
      )
      setNovoNome('')
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível criar a subcategoria.'))
    } finally {
      setCriando(false)
    }
  }

  const iniciarEdicao = (sub: SubCategoriaResponse) => {
    setExcluindoId(null)
    setEditandoId(sub.id)
    setEditandoNome(sub.nome)
    setErro('')
  }

  // PUT — salva o novo nome
  const salvarEdicao = async () => {
    if (editandoId === null) return
    setErro('')
    setEditandoBtn(true)
    try {
      const atualizada = await editarSubCategoria(editandoId, { nome: editandoNome.trim() })
      setSubcategorias((prev) =>
        prev
          .map((s) => (s.id === atualizada.id ? atualizada : s))
          .sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')),
      )
      setEditandoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível editar a subcategoria.'))
    } finally {
      setEditandoBtn(false)
    }
  }

  const pedirExclusao = (id: number) => {
    setEditandoId(null)
    setExcluindoId((prev) => (prev === id ? null : id))
    setErro('')
  }

  // DELETE — confirma a exclusão
  const confirmarExclusao = async () => {
    if (excluindoId === null) return
    setErro('')
    setExcluindoBtn(true)
    try {
      await excluirSubCategoria(excluindoId)
      setSubcategorias((prev) => prev.filter((s) => s.id !== excluindoId))
      setExcluindoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível excluir a subcategoria.'))
    } finally {
      setExcluindoBtn(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Editar Subcategorias"
      subtitulo="Crie, renomeie e exclua as subcategorias da conta selecionada."
      aoFechar={aoFechar}
      largura="lg"
    >
      <div className="gerenciar-sub">
        {/* Formulário de criação (POST) */}
        <form className="formulario" onSubmit={handleCriar}>
          <div className="gerenciar-sub-nova">
            <label className="campo">
              Nome
              <input
                type="text"
                value={novoNome}
                onChange={(e) => setNovoNome(e.target.value)}
                placeholder="Ex: Supermercado, Combustível..."
                minLength={2}
                maxLength={100}
                required
                autoFocus={aberto}
              />
            </label>
            <label className="campo">
              Conta
              <select
                value={contaSelecionadaChave}
                onChange={(e) => setContaSelecionadaChave(e.target.value)}
                required
              >
                {contas.length === 0 && <option value="">Carregando contas...</option>}
                <OpcoesDeConta opcoes={contas} />
              </select>
            </label>
            <button type="submit" className="btn btn-primario" disabled={criando}>
              {criando ? 'Criando...' : 'Criar'}
            </button>
          </div>
        </form>

        {erro && <p className="erro">{erro}</p>}

        {/* Lista (GET) */}
        <div className="gerenciar-sub-lista">
          {carregando ? (
            <div className="estado-vazio">Carregando subcategorias...</div>
          ) : subcategorias.length === 0 ? (
            <div className="estado-vazio">
              Nenhuma subcategoria nesta conta.
              <button type="button" className="btn btn-ghost" onClick={() => setRecarga((n) => n + 1)}>
                Atualizar
              </button>
            </div>
          ) : (
            subcategorias.map((sub) => (
              <div key={sub.id} className="gerenciar-sub-item">
                {editandoId === sub.id ? (
                  <div className="gerenciar-sub-editar">
                    <input
                      type="text"
                      value={editandoNome}
                      onChange={(e) => setEditandoNome(e.target.value)}
                      minLength={2}
                      maxLength={100}
                      autoFocus
                    />
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
                ) : (
                  <>
                    <div className="gerenciar-sub-info">
                      <span className="gerenciar-sub-nome">{sub.nome}</span>
                    </div>
                    <div className="gerenciar-sub-acoes">
                      <button
                        type="button"
                        className="gerenciar-sub-icone"
                        aria-label="Editar"
                        title="Editar"
                        onClick={() => iniciarEdicao(sub)}
                      >
                        <IconeLapis />
                      </button>
                      <button
                        type="button"
                        className="gerenciar-sub-icone perigo"
                        aria-label="Excluir"
                        title="Excluir"
                        onClick={() => pedirExclusao(sub.id)}
                      >
                        <IconeLixeira />
                      </button>
                    </div>
                  </>
                )}

                {excluindoId === sub.id && (
                  <div className="gerenciar-sub-confirma">
                    <div className="gerenciar-sub-confirma-texto">
                      <span>Excluir "{sub.nome}"?</span>
                      <span className="gerenciar-sub-confirma-adendo">
                        As transações dela continuam, sem subcategoria.
                      </span>
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
