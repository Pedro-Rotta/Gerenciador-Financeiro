import { useState, type FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import { extrairErro } from '../utils/erros'

function Login() {
  const aviso = (useLocation().state as { aviso?: string } | null)?.aviso
  const [nomeDeUsuario, setNomeDeUsuario] = useState('')
  const [senha, setSenha] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)
  const { login } = useAuth()
  const navigate = useNavigate()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErro('')
    setCarregando(true)
    try {
      await login(nomeDeUsuario, senha)
      navigate('/dashboard')
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Nome de usuário ou senha inválidos.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={handleSubmit}>
        <h1>Gerenciador Familiar</h1>
        <p className="auth-subtitle">Faça login para continuar</p>

        <label>
          Nome de usuário
          <input
            type="text"
            value={nomeDeUsuario}
            onChange={(e) => setNomeDeUsuario(e.target.value)}
            required
            autoFocus
          />
        </label>

        <label>
          Senha
          <input
            type="password"
            value={senha}
            onChange={(e) => setSenha(e.target.value)}
            required
          />
        </label>

        {/* Vindo do "Esqueceu a senha": "Senha alterada. Entre com a nova senha." */}
        {aviso && !erro && <p className="sucesso">{aviso}</p>}
        {erro && <p className="erro">{erro}</p>}

        <button type="submit" disabled={carregando}>
          {carregando ? 'Entrando...' : 'Entrar'}
        </button>

        <p className="auth-link">
          Não tem conta? <Link to="/cadastro">Cadastre-se</Link>
        </p>
        <p className="auth-link">
          Esqueceu a senha? <Link to="/recuperar-senha">Nova senha</Link>
        </p>
      </form>
    </div>
  )
}

export default Login