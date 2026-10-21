/**
 * Tipos dos Relatórios, alinhados às DTOs do backend (.NET).
 */

/** Item das despesas do mês ("Top 5 Maiores Despesas do Mês") */
export interface RelatorioDespesaItem {
  descricao: string
  categoria: string
  valor: number
  dataTransacao: string
}

/** Linha do "Comparativo Mensal" */
export interface RelatorioMesItem {
  ano: number
  mes: number
  receita: number
  despesa: number
  resultado: number
  /** Variação % do resultado vs. mês anterior (null no 1º mês ou quando o anterior foi zero) */
  variacaoPercentual: number | null
}

/** Meses do período do relatório: mês, trimestre, semestre ou ano */
export type MesesPeriodo = 1 | 3 | 6 | 12

/**
 * GET api/Relatorio/resumo?ano=X&mes=Y&meses=N — período de N meses terminando em ano/mes.
 * Os campos "...Mes" valem para o período inteiro (o nome ficou por compatibilidade).
 */
export interface RelatorioResumoResponse {
  /** Último mês do período */
  ano: number
  mes: number
  meses: MesesPeriodo
  /** Primeiro e último dia do período ("yyyy-MM-dd") */
  dataInicio: string
  dataFim: string
  mediaGastosDiarios: number
  /** Média diária do período anterior, de mesmo tamanho */
  mediaGastosDiariosMesAnterior: number
  receitasMes: number
  despesasMes: number
  /** % da receita que sobrou no período (null quando não houve receita) */
  economiaPercentual: number | null
  maiorCategoriaNome: string | null
  maiorCategoriaValor: number
  /** Quanto guardar por mês para concluir as metas em andamento no prazo (situação atual das metas) */
  aporteMensalMetas: number
  /** Quantidade de metas em andamento (não concluídas) */
  metasEmAndamento: number
  /** Todas as saídas do mês, da maior para a menor (a tela exibe as 5 primeiras) */
  despesas: RelatorioDespesaItem[]
  /** Do mês da primeira transação até o mês do relatório (no mínimo 6 meses) */
  comparativo: RelatorioMesItem[]
}
