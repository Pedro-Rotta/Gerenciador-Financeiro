/**
 * FiltrosTransacoes — barra de filtros (tipo, período, conta e categoria)
 * usada na tabela de transações do Dashboard e na página de Categorias.
 * O componente só desenha os controles; quem o usa aplica o filtro com
 * `intervaloDoPeriodo` / `filtrarTransacoes` (utils/filtrosTransacoes).
 */

import { IconeFiltro } from './icons'
import { dataLocalISO } from '../utils/data'
import { PERIODOS, type FiltroPeriodo, type FiltroTipo, type Filtros, type OpcaoCategoria } from '../utils/filtrosTransacoes'
import type { OpcaoConta } from '../services/opcoesConta'

interface FiltrosTransacoesProps {
  valor: Filtros
  aoMudar: (filtros: Filtros) => void
  contas: OpcaoConta[]
  categorias: OpcaoCategoria[]
  /** Filtros iniciais: quando diferentes do valor atual, aparece "Limpar". */
  padrao: Filtros
  /** Mostra a opção "Todas as contas" (a página de Categorias exige uma conta). */
  permitirTodasContas?: boolean
}

export default function FiltrosTransacoes({
  valor,
  aoMudar,
  contas,
  categorias,
  padrao,
  permitirTodasContas = true,
}: FiltrosTransacoesProps) {
  const hoje = dataLocalISO()

  // Categorias compatíveis com o tipo escolhido
  const categoriasDoTipo = categorias.filter(
    (c) =>
      valor.tipo === 'todas' ||
      (valor.tipo === 'entradas' ? c.tipo === 'entrada' : c.tipo === 'saida'),
  )
  // A categoria escolhida continua na lista mesmo que não exista na conta atual,
  // para o <select> mostrar o filtro que está de fato aplicado
  const nomesCategorias = Array.from(
    new Set([...categoriasDoTipo.map((c) => c.nome), ...(valor.categoria ? [valor.categoria] : [])]),
  ).sort((a, b) => a.localeCompare(b, 'pt-BR'))

  const mudar = (parcial: Partial<Filtros>) => {
    const novo = { ...valor, ...parcial }
    // Tipo trocado: descarta a categoria se ela não existe no novo tipo
    if (parcial.tipo && novo.categoria) {
      const existe = categorias.some(
        (c) =>
          c.nome === novo.categoria &&
          (novo.tipo === 'todas' || c.tipo === (novo.tipo === 'entradas' ? 'entrada' : 'saida')),
      )
      if (!existe) novo.categoria = ''
    }
    aoMudar(novo)
  }

  const alterado = (Object.keys(padrao) as (keyof Filtros)[]).some((k) => padrao[k] !== valor[k])

  const grupos = [
    { titulo: 'Contas', lista: contas.filter((c) => !c.conjunta) },
    { titulo: 'Contas Conjuntas', lista: contas.filter((c) => c.conjunta) },
  ].filter((g) => g.lista.length > 0)

  return (
    <div className="filtros-transacoes" role="group" aria-label="Filtros">
      <span className="rel-filtros-icone" aria-hidden="true">
        <IconeFiltro />
      </span>

      <div className="btn-grupo" role="group" aria-label="Tipo">
        {(
          [
            ['todas', 'Todas'],
            ['entradas', 'Entradas'],
            ['saidas', 'Saídas'],
          ] as [FiltroTipo, string][]
        ).map(([tipo, rotulo]) => (
          <button
            key={tipo}
            type="button"
            className={valor.tipo === tipo ? 'ativo' : ''}
            aria-pressed={valor.tipo === tipo}
            onClick={() => mudar({ tipo })}
          >
            {rotulo}
          </button>
        ))}
      </div>

      <select
        className="rel-select"
        aria-label="Período"
        value={valor.periodo}
        onChange={(e) => mudar({ periodo: e.target.value as FiltroPeriodo })}
      >
        {PERIODOS.map((p) => (
          <option key={p.valor} value={p.valor}>
            {p.rotulo}
          </option>
        ))}
      </select>

      {valor.periodo === 'personalizado' && (
        <span className="filtros-transacoes-datas">
          <input
            type="date"
            className="rel-select"
            aria-label="Data inicial"
            value={valor.de}
            max={valor.ate || hoje}
            onChange={(e) => mudar({ de: e.target.value })}
          />
          <span className="filtros-transacoes-ate">até</span>
          <input
            type="date"
            className="rel-select"
            aria-label="Data final"
            value={valor.ate}
            min={valor.de || undefined}
            max={hoje}
            onChange={(e) => mudar({ ate: e.target.value })}
          />
        </span>
      )}

      <select
        className="rel-select"
        aria-label="Conta"
        value={valor.conta}
        onChange={(e) => mudar({ conta: e.target.value })}
      >
        {permitirTodasContas && <option value="">Todas as contas</option>}
        {grupos.map((g) => (
          <optgroup key={g.titulo} label={g.titulo}>
            {g.lista.map((c) => (
              <option key={c.chave} value={c.chave}>
                {c.principal ? `${c.nome} (principal)` : c.nome}
              </option>
            ))}
          </optgroup>
        ))}
      </select>

      <select
        className="rel-select"
        aria-label="Categoria"
        value={valor.categoria}
        onChange={(e) => mudar({ categoria: e.target.value })}
      >
        <option value="">Todas as categorias</option>
        {nomesCategorias.map((nome) => (
          <option key={nome} value={nome}>
            {nome}
          </option>
        ))}
      </select>

      {alterado && (
        <button type="button" className="btn btn-ghost filtros-transacoes-limpar" onClick={() => aoMudar(padrao)}>
          Limpar
        </button>
      )}
    </div>
  )
}
