import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { criarMembro } from '../services/membros'
import type { CriarMembroRequest } from '../types/membro'
import { extrairErro } from '../utils/erros'
const MENSAGEM_SENHAS_DIFERENTES = 'As senhas não coincidem. Digite a mesma senha nos dois campos.'

function Cadastro() {
  const [form, setForm] = useState<CriarMembroRequest>({
    nome: '',
    nomeDeUsuario: '',
    email: '',
    senha: '',
    confirmacaoSenha: '',
  })
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)
  const navigate = useNavigate()

  const atualizar = (campo: keyof CriarMembroRequest, valor: string) => {
    setForm((prev) => ({ ...prev, [campo]: valor }))
    // Some com o aviso de senhas diferentes assim que a pessoa corrige
    if (erro === MENSAGEM_SENHAS_DIFERENTES) setErro('')
  }

  // Aviso na hora, só depois que a confirmação começou a ser digitada
  const senhasDiferentes = form.confirmacaoSenha.length > 0 && form.senha !== form.confirmacaoSenha

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErro('')
    // Dupla validação da senha: só envia se as duas forem idênticas
    if (form.senha !== form.confirmacaoSenha) {
      setErro(MENSAGEM_SENHAS_DIFERENTES)
      return
    }
    setCarregando(true)
    try {
      await criarMembro(form)
      navigate('/login')
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível criar a conta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h1>Criar conta</h1>
        <p className="auth-subtitle">Cadastre-se para começar</p>

        <label>
          Nome
          <input
            type="text"
            value={form.nome}
            onChange={(e) => atualizar('nome', e.target.value)}
            required
          />
        </label>

        <label>
          Nome de usuário
          <input
            type="text"
            value={form.nomeDeUsuario}
            onChange={(e) => atualizar('nomeDeUsuario', e.target.value)}
            required
          />
        </label>

        <label>
          E-mail
          <input
            type="email"
            value={form.email}
            onChange={(e) => atualizar('email', e.target.value)}
            required
          />
        </label>

        <label>
          Senha
          <input
            type="password"
            value={form.senha}
            onChange={(e) => atualizar('senha', e.target.value)}
            required
            minLength={6}
            autoComplete="new-password"
          />
        </label>

        <label>
          Confirmar senha
          <input
            type="password"
            value={form.confirmacaoSenha}
            onChange={(e) => atualizar('confirmacaoSenha', e.target.value)}
            required
            minLength={6}
            autoComplete="new-password"
            aria-invalid={senhasDiferentes}
            aria-describedby={senhasDiferentes ? 'aviso-senhas' : undefined}
            className={senhasDiferentes ? 'invalido' : undefined}
          />
          {senhasDiferentes && (
            <span id="aviso-senhas" className="auth-aviso-campo" role="alert">
              As senhas não coincidem.
            </span>
          )}
        </label>

        {erro && <p className="erro">{erro}</p>}

        <button type="submit" disabled={carregando}>
          {carregando ? 'Criando...' : 'Cadastrar'}
        </button>

        <p className="auth-link">
          Já tem conta? <Link to="/login">Entrar</Link>
        </p>
      </form>
    </div>
  )
}

export default Cadastro
