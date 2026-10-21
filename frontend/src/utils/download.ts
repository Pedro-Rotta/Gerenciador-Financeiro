/**
 * Download de arquivos gerados (PDF, Excel) sem abrir janela.
 */

/** Baixa um arquivo gerado no navegador ou recebido da API. */
export function baixarArquivo(conteudo: Blob, nomeArquivo: string) {
  const url = URL.createObjectURL(conteudo)
  const link = document.createElement('a')
  link.href = url
  link.download = nomeArquivo
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

/**
 * Nome sugerido pelo servidor no cabeçalho Content-Disposition
 * ("attachment; filename=relatorio-2026-10.pdf; filename*=UTF-8''...").
 */
export function nomeDoArquivo(contentDisposition: unknown, padrao: string): string {
  return /filename="?([^";]+)"?/i.exec(String(contentDisposition ?? ''))?.[1] ?? padrao
}
