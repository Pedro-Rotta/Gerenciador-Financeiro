/**
 * Serviço de API das séries recorrentes.
 * Todas as chamadas levam o cookie de sessão (enviado pelo navegador).
 */

import api from './api'
import type { OcorrenciaPrevistaResponse } from '../types/recorrencia'

/** GET api/Recorrencia/previstas — mensais do mês atual e do próximo ainda não lançadas */
export async function listarOcorrenciasPrevistas(): Promise<OcorrenciaPrevistaResponse[]> {
  const { data } = await api.get<OcorrenciaPrevistaResponse[]>('/Recorrencia/previstas')
  return data
}
