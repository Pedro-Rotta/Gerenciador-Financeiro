/**
 * Serviço de API para Relatórios.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import { nomeDoArquivo } from '../utils/download'
import type { MesesPeriodo, RelatorioResumoResponse } from '../types/relatorio'

/**
 * GET api/Relatorio/resumo?ano=X&mes=Y&meses=N — resumo financeiro do período de
 * N meses terminando em ano/mes (cards, despesas do período e comparativo mensal).
 */
export async function obterResumoRelatorio(
  ano: number,
  mes: number,
  meses: MesesPeriodo = 1,
): Promise<RelatorioResumoResponse> {
  const { data } = await api.get<RelatorioResumoResponse>('/Relatorio/resumo', {
    params: { ano, mes, meses },
  })
  // Backend sem suporte a período (versão anterior) não envia "meses": os dados são
  // de um mês só, então a tela não pode tratá-los como trimestre/semestre/ano
  return { ...data, meses: data.meses ?? 1 }
}

/**
 * GET api/Relatorio/resumo/pdf?ano=X&mes=Y&meses=N — PDF do mesmo resumo, gerado no
 * backend (QuestPDF). Devolve o arquivo e o nome sugerido pelo servidor.
 */
export async function baixarPdfRelatorio(
  ano: number,
  mes: number,
  meses: MesesPeriodo = 1,
): Promise<{ arquivo: Blob; nomeArquivo: string }> {
  const resposta = await api.get<Blob>('/Relatorio/resumo/pdf', {
    params: { ano, mes, meses },
    responseType: 'blob',
  })
  return {
    arquivo: resposta.data,
    nomeArquivo: nomeDoArquivo(
      resposta.headers['content-disposition'],
      `relatorio-${ano}-${String(mes).padStart(2, '0')}.pdf`,
    ),
  }
}
