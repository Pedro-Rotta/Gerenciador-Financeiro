/**
 * Utilitários de data no fuso horário LOCAL do navegador.
 * Evita `toISOString()`, que converte para UTC: à noite no Brasil (UTC-3),
 * ele já devolve o dia seguinte.
 */

/** "agora", "há 5 min", "há 3 h", "ontem", "há 4 dias" a partir de uma data ISO. */
export function tempoRelativo(dataIso: string): string {
  const minutos = Math.max(0, Math.round((Date.now() - new Date(dataIso).getTime()) / 60_000))
  if (minutos < 1) return 'agora'
  if (minutos < 60) return `há ${minutos} min`
  const horas = Math.floor(minutos / 60)
  if (horas < 24) return `há ${horas} h`
  const dias = Math.floor(horas / 24)
  return dias === 1 ? 'ontem' : `há ${dias} dias`
}

/** Data local no formato "yyyy-MM-dd" (o mesmo do <input type="date"> e do DateOnly do backend). */
export function dataLocalISO(data: Date = new Date()): string {
  const ano = data.getFullYear()
  const mes = String(data.getMonth() + 1).padStart(2, '0')
  const dia = String(data.getDate()).padStart(2, '0')
  return `${ano}-${mes}-${dia}`
}
