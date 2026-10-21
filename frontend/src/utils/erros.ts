/**
 * Mensagem de erro de uma chamada à API, na ordem:
 * 1. `mensagem` do ErroApiResponse (middleware do backend e limite de tentativas);
 * 2. primeiro erro de validação do ASP.NET (`errors`);
 * 3. `title` da validação;
 * 4. o texto padrão informado por quem chama.
 */
export function extrairErro(err: unknown, padrao: string): string {
  const dados = (err as { response?: { data?: unknown } }).response?.data
  if (typeof dados === 'object' && dados !== null) {
    if ('mensagem' in dados) return (dados as { mensagem: string }).mensagem
    const erros = (dados as { errors?: Record<string, string[]> }).errors
    const primeiro = erros && Object.values(erros)[0]?.[0]
    if (primeiro) return primeiro
    const titulo = (dados as { title?: string }).title
    if (titulo) return titulo
  }
  return padrao
}
