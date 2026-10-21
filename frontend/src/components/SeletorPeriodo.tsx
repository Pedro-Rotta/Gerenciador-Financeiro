/**
 * SeletorPeriodo — botões de período (ex.: Semana/Mês/Tri/Semestre/Personalizado)
 * com as datas de início e fim quando o período é personalizado.
 * Usado no painel "Gastos por categoria" do Dashboard.
 */

import { dataLocalISO } from '../utils/data'
import type { FiltroPeriodo, Periodo } from '../utils/filtrosTransacoes'

interface SeletorPeriodoProps {
  valor: Periodo
  aoMudar: (periodo: Periodo) => void
  /** Períodos oferecidos, na ordem dos botões */
  opcoes: { valor: FiltroPeriodo; rotulo: string }[]
}

export default function SeletorPeriodo({ valor, aoMudar, opcoes }: SeletorPeriodoProps) {
  const hoje = dataLocalISO()

  return (
    <div className="seletor-periodo">
      <div className="btn-grupo" role="group" aria-label="Período">
        {opcoes.map((o) => (
          <button
            key={o.valor}
            type="button"
            className={valor.periodo === o.valor ? 'ativo' : ''}
            aria-pressed={valor.periodo === o.valor}
            onClick={() => aoMudar({ ...valor, periodo: o.valor })}
          >
            {o.rotulo}
          </button>
        ))}
      </div>

      {valor.periodo === 'personalizado' && (
        <span className="filtros-transacoes-datas">
          <input
            type="date"
            className="rel-select"
            aria-label="Data inicial"
            value={valor.de}
            max={valor.ate || hoje}
            onChange={(e) => aoMudar({ ...valor, de: e.target.value })}
          />
          <span className="filtros-transacoes-ate">até</span>
          <input
            type="date"
            className="rel-select"
            aria-label="Data final"
            value={valor.ate}
            min={valor.de || undefined}
            max={hoje}
            onChange={(e) => aoMudar({ ...valor, ate: e.target.value })}
          />
        </span>
      )}
    </div>
  )
}
