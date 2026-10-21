/**
 * SessaoExpiradaModal — Aviso exibido quando o login (token JWT) expira.
 * Cobre a tela inteira; fecha no X, clicando fora ou com Escape (via Modal).
 * Ao fechar, a sessão é encerrada e o usuário volta para a tela de login.
 */

import Modal from './Modal'

interface SessaoExpiradaModalProps {
  aberto: boolean
  aoFechar: () => void
}

export default function SessaoExpiradaModal({ aberto, aoFechar }: SessaoExpiradaModalProps) {
  return (
    <Modal
      aberto={aberto}
      titulo="Sessão expirada"
      subtitulo="Por segurança, o login vale por um tempo limitado."
      aoFechar={aoFechar}
    >
      <div className="formulario">
        <p>Sua sessão expirou. Entre novamente para continuar usando o sistema.</p>

        <div className="formulario-acoes">
          <button type="button" className="btn btn-primario" onClick={aoFechar}>
            Entrar novamente
          </button>
        </div>
      </div>
    </Modal>
  )
}
