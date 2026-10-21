/**
 * "Esqueceu a senha": passo 1 pede o código (por e-mail), passo 2 troca a senha com ele.
 * A resposta do passo 1 é sempre a mesma, exista ou não a conta.
 */

import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { pedirCodigoSenha, redefinirSenha } from '../services/auth'
import { extrairErro } from '../utils/erros'
function RecuperarSenha() {
  const [passo, setPasso] = useState<1 | 2>(1)
  const [identificador, setIdentificador] = useState('')
  const [codigo, setCodigo] = useState('')
  const [novaSenha, setNovaSenha] = useState('')
  const [confirmacaoSenha, setConfirmacaoSenha] = useState('')
  const [aviso, setAviso] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)
  const navigate = useNavigate()

  const enviarCodigo = async (e?: FormEvent) => {
    e?.preventDefault()
    // Clique duplo no "Reenviar código" não manda dois códigos
    if (carregando) return
    setErro('')
    setCarregando(true)
    try {
      setAviso(await pedirCodigoSenha(identificador))
      setPasso(2)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível enviar o código.'))
    } finally {
      setCarregando(false)
    }
  }

  const salvar = async (e: FormEvent) => {
    e.preventDefault()
    setErro('')
    if (novaSenha !== confirmacaoSenha) {
      setErro('As senhas não coincidem')
      return
    }
    setCarregando(true)
    try {
      const mensagem = await redefinirSenha({ identificador, codigo, novaSenha, confirmacaoSenha })
      navigate('/login', { state: { aviso: mensagem } })
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível trocar a senha.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div className="auth-page">
      {passo === 1 ? (
        <form className="auth-card" onSubmit={enviarCodigo}>
          <h1>Nova senha</h1>
          <p className="auth-subtitle">Enviamos um código para o e-mail da sua conta.</p>

          <label>
            Nome de usuário ou e-mail
            <input
              type="text"
              value={identificador}
              onChange={(e) => setIdentificador(e.target.value)}
              required
              autoFocus
            />
          </label>

          {erro && <p className="erro">{erro}</p>}

          <button type="submit" disabled={carregando}>
            {carregando ? 'Enviando...' : 'Enviar código'}
          </button>

          <p className="auth-link">
            Lembrou a senha? <Link to="/login">Entrar</Link>
          </p>
        </form>
      ) : (
        <form className="auth-card" onSubmit={salvar}>
          <h1>Nova senha</h1>
          <p className="auth-subtitle">{aviso} O código vale por 15 minutos.</p>

          <label>
            Código (6 números)
            <input
              type="text"
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={6}
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.replace(/\D/g, ''))}
              required
              autoFocus
            />
          </label>

          <label>
            Nova senha
            <input
              type="password"
              autoComplete="new-password"
              minLength={6}
              maxLength={100}
              value={novaSenha}
              onChange={(e) => setNovaSenha(e.target.value)}
              required
            />
          </label>

          <label>
            Confirmar nova senha
            <input
              type="password"
              autoComplete="new-password"
              value={confirmacaoSenha}
              onChange={(e) => setConfirmacaoSenha(e.target.value)}
              required
            />
          </label>

          {erro && <p className="erro">{erro}</p>}

          <button type="submit" disabled={carregando}>
            {carregando ? 'Salvando...' : 'Salvar nova senha'}
          </button>

          <p className="auth-link">
            Não chegou?{' '}
            <a
              href="#"
              onClick={(e) => {
                e.preventDefault()
                enviarCodigo()
              }}
            >
              {carregando ? 'Enviando...' : 'Reenviar código'}
            </a>
            {' · '}
            <Link to="/login">Voltar ao login</Link>
          </p>
        </form>
      )}
    </div>
  )
}

export default RecuperarSenha
