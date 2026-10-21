/**
 * OpcoesDeConta — <option>s de um <select> de conta, separados em
 * "Contas" (individuais) e "Contas Conjuntas".
 * Com `paraLancamento`, contas sem saldo inicial aparecem desativadas (não aceitam transações).
 */

import type { OpcaoConta } from '../services/opcoesConta'

export default function OpcoesDeConta({
  opcoes,
  paraLancamento = false,
}: {
  opcoes: OpcaoConta[]
  paraLancamento?: boolean
}) {
  const individuais = opcoes.filter((o) => !o.conjunta)
  const conjuntas = opcoes.filter((o) => o.conjunta)
  return (
    <>
      <optgroup label="Contas">
        {individuais.map((o) => {
          const bloqueada = paraLancamento && !o.liberada
          return (
            <option key={o.chave} value={o.chave} disabled={bloqueada}>
              {o.nome}
              {bloqueada && ' (defina o saldo inicial)'}
            </option>
          )
        })}
      </optgroup>
      {conjuntas.length > 0 && (
        <optgroup label="Contas Conjuntas">
          {conjuntas.map((o) => (
            <option key={o.chave} value={o.chave}>
              {o.nome}
            </option>
          ))}
        </optgroup>
      )}
    </>
  )
}
