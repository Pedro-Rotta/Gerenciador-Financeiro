/**
 * ContasConjuntas — Seção de Contas Conjuntas (compartilhadas entre membros).
 * Sincronizada com o backend: GET (listar), POST (criar), PUT (editar) e DELETE (excluir).
 * Só o criador da conta pode editá-la, excluí-la e remover participantes; os demais
 * participantes visualizam e podem sair da conta.
 */

import { useState, useEffect, useCallback, useRef, type FormEvent } from 'react'
import {
  IconeUsuarios,
  IconeEllipsisVertical,
  IconeMais,
  IconeCarteira,
  IconeSetaCima,
  IconeSetaBaixo,
  IconeLapis,
  IconeLixeira,
} from '../components/icons'
import Modal from '../components/Modal'
import { useAuth } from '../context/useAuth'
import {
  listarContasConjuntas,
  criarContaConjunta,
  editarContaConjunta,
  excluirContaConjunta,
  removerParticipante,
  sairContaConjunta,
} from '../services/contasConjuntas'
import {
  criarConvite,
  listarConvitesDaConta,
  cancelarConvite,
  EVENTO_CONTAS_CONJUNTAS_ALTERADAS,
} from '../services/convites'
import { listarTransacoes } from '../services/transacoes'
import type { ContaConjuntaResponse } from '../types/contaConjunta'
import type { ConviteResponse } from '../types/convite'
import { TipoTransacao } from '../types/transacao'
import type { TransacaoResponse } from '../types/transacao'
import { extrairErro } from '../utils/erros'
/* ---------- Helpers ---------- */

function moeda(valor: number) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-')
  if (!ano || !mes || !dia) return data
  return `${dia}/${mes}/${ano}`
}

function quantidadeParticipantes(conta: ContaConjuntaResponse) {
  const n = (conta.membros ?? []).length
  return `${n} participante${n === 1 ? '' : 's'}`
}

/* ---------- Menu de opções (três pontos) ---------- */

interface OpcoesProps {
  onEditar?: () => void
  onConvidar?: () => void
  onDetalhes?: () => void
  onExcluir?: () => void
  onSair?: () => void
}

// Mostra apenas as opções recebidas (quem não é o criador vê "Ver Detalhes" e "Sair da conta conjunta").
function MenuOpcoes({ onEditar, onConvidar, onDetalhes, onExcluir, onSair }: OpcoesProps) {
  const [aberto, setAberto] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  // Fecha o menu ao clicar fora dele
  useEffect(() => {
    if (!aberto) return
    const aoClicarFora = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setAberto(false)
      }
    }
    document.addEventListener('mousedown', aoClicarFora)
    return () => document.removeEventListener('mousedown', aoClicarFora)
  }, [aberto])

  return (
    <div className="conta-menu" ref={containerRef}>
      <button
        type="button"
        className="conta-menu-btn"
        aria-label="Mais opções"
        onClick={(e) => {
          e.stopPropagation()
          setAberto((v) => !v)
        }}
      >
        <IconeEllipsisVertical />
      </button>
      {aberto && (
        <div className="conta-menu-dropdown">
          {onEditar && (
            <button type="button" className="conta-menu-item" onClick={() => { setAberto(false); onEditar() }}>
              Editar
            </button>
          )}
          {onConvidar && (
            <button type="button" className="conta-menu-item" onClick={() => { setAberto(false); onConvidar() }}>
              Convidar
            </button>
          )}
          {onDetalhes && (
            <button type="button" className="conta-menu-item" onClick={() => { setAberto(false); onDetalhes() }}>
              Ver Detalhes
            </button>
          )}
          {onExcluir && (
            <button type="button" className="conta-menu-item" onClick={() => { setAberto(false); onExcluir() }}>
              Excluir
            </button>
          )}
          {onSair && (
            <button type="button" className="conta-menu-item" onClick={() => { setAberto(false); onSair() }}>
              Sair da conta conjunta
            </button>
          )}
        </div>
      )}
    </div>
  )
}

/* ---------- Cartão de Conta Conjunta ---------- */

function CardContaConjunta({
  conta,
  ehCriador,
  onEditar,
  onConvidar,
  onExcluir,
  onDetalhes,
  onSair,
}: {
  conta: ContaConjuntaResponse
  ehCriador: boolean
  onEditar: (conta: ContaConjuntaResponse) => void
  onConvidar: (conta: ContaConjuntaResponse) => void
  onExcluir: (conta: ContaConjuntaResponse) => void
  onDetalhes: (conta: ContaConjuntaResponse) => void
  onSair: (conta: ContaConjuntaResponse) => void
}) {
  const { nome, descricao, saldo, saldoPrevisto } = conta
  const [totais, setTotais] = useState<{ entrada: number; despesa: number }>({
    entrada: 0,
    despesa: 0,
  })

  useEffect(() => {
    let ativo = true
    listarTransacoes({ contaConjuntaId: conta.id })
      .then((transacoes) => {
        if (!ativo) return
        const entrada = transacoes
          .filter((t) => t.tipo === TipoTransacao.ENTRADA)
          .reduce((acc, t) => acc + t.valor, 0)
        const despesa = transacoes
          .filter((t) => t.tipo === TipoTransacao.SAIDA)
          .reduce((acc, t) => acc + t.valor, 0)
        setTotais({ entrada, despesa })
      })
      .catch(() => {
        if (ativo) setTotais({ entrada: 0, despesa: 0 })
      })
    return () => {
      ativo = false
    }
  }, [conta.id])

  return (
    <div className="conta-card">
      <div className="conta-card-topo">
        <span className="conta-card-icone conjunta">
          <IconeUsuarios />
        </span>
        <MenuOpcoes
          onEditar={ehCriador ? () => onEditar(conta) : undefined}
          onConvidar={ehCriador ? () => onConvidar(conta) : undefined}
          onDetalhes={() => onDetalhes(conta)}
          onExcluir={ehCriador ? () => onExcluir(conta) : undefined}
          onSair={ehCriador ? undefined : () => onSair(conta)}
        />
      </div>

      <div className="conta-card-nome">{nome}</div>
      {descricao && <div className="conta-card-sub">{descricao}</div>}

      <div className="conta-card-saldo">{moeda(saldo)}</div>
      <div className="conta-card-previsto">Previsto: {moeda(saldoPrevisto ?? saldo)}</div>

      <div className="conta-card-fluxo">
        <span className="conta-card-fluxo-item despesa">
          <span className="conta-card-fluxo-icone despesa">
            <IconeSetaBaixo />
          </span>
          <span>{moeda(totais.despesa)}</span>
        </span>
        <span className="conta-card-fluxo-item entrada">
          <span className="conta-card-fluxo-icone entrada">
            <IconeSetaCima />
          </span>
          <span>{moeda(totais.entrada)}</span>
        </span>
      </div>

      <div className="conta-card-info">
        <span className={`tag ${ehCriador ? 'verde' : 'fosco'}`}>
          {ehCriador ? 'Criada por você' : 'Compartilhada'}
        </span>
        <span className="conta-card-update">{quantidadeParticipantes(conta)}</span>
      </div>
    </div>
  )
}

/* ---------- Cabeçalho de seção com botão de ação ---------- */

function CabecalhoSecao({
  titulo,
  acao,
  onAcao,
  acaoSecundaria,
  onAcaoSecundaria,
}: {
  titulo: string
  acao: string
  onAcao?: () => void
  acaoSecundaria?: string
  onAcaoSecundaria?: () => void
}) {
  return (
    <div className="conta-secao-cabecalho">
      <h2>{titulo}</h2>
      <div className="conta-secao-acoes">
        <button type="button" className="btn btn-primario" onClick={onAcao}>
          <IconeMais />
          <span className="btn-label">{acao}</span>
        </button>
        {acaoSecundaria && (
          <button type="button" className="btn btn-primario" onClick={onAcaoSecundaria}>
            <IconeLapis />
            <span className="btn-label">{acaoSecundaria}</span>
          </button>
        )}
      </div>
    </div>
  )
}

/* ---------- Página principal ---------- */

export default function ContasConjuntas() {
  const { membroId } = useAuth()
  const [contas, setContas] = useState<ContaConjuntaResponse[]>([])
  const [carregando, setCarregando] = useState(true)
  const [erroBusca, setErroBusca] = useState('')
  const [modalNovaConta, setModalNovaConta] = useState(false)
  const [modalGerenciarContas, setModalGerenciarContas] = useState(false)
  const [contaEditar, setContaEditar] = useState<ContaConjuntaResponse | null>(null)
  const [contaExcluir, setContaExcluir] = useState<ContaConjuntaResponse | null>(null)
  const [contaDetalhes, setContaDetalhes] = useState<ContaConjuntaResponse | null>(null)
  const [contaConvidar, setContaConvidar] = useState<ContaConjuntaResponse | null>(null)
  const [contaSair, setContaSair] = useState<ContaConjuntaResponse | null>(null)

  const carregarContas = useCallback(async () => {
    setCarregando(true)
    setErroBusca('')
    try {
      const data = await listarContasConjuntas()
      setContas(data)
    } catch (err: unknown) {
      setErroBusca(extrairErro(err, 'Não foi possível carregar as contas conjuntas.'))
    } finally {
      setCarregando(false)
    }
  }, [])

  useEffect(() => {
    carregarContas()
  }, [carregarContas])

  // Recarrega quando o membro entra numa conta conjunta (convite aceito no sino)
  useEffect(() => {
    window.addEventListener(EVENTO_CONTAS_CONJUNTAS_ALTERADAS, carregarContas)
    return () => window.removeEventListener(EVENTO_CONTAS_CONJUNTAS_ALTERADAS, carregarContas)
  }, [carregarContas])

  const ehCriador = (conta: ContaConjuntaResponse) => conta.criadorId === membroId

  const adicionarConta = (conta: ContaConjuntaResponse) => {
    setContas((prev) => [...prev, conta])
  }

  const atualizarConta = (atualizada: ContaConjuntaResponse) => {
    setContas((prev) => prev.map((c) => (c.id === atualizada.id ? atualizada : c)))
  }

  const removerConta = (id: number) => {
    setContas((prev) => prev.filter((c) => c.id !== id))
  }

  const saldoTotal = contas.reduce((acc, c) => acc + c.saldo, 0)

  return (
    <div className="pagina">
      <div className="pagina-cabecalho">
        <div>
          <h1 className="pagina-titulo">Contas Conjuntas</h1>
          <p className="pagina-subtitulo">Gerencie as contas compartilhadas com a sua família.</p>
        </div>
      </div>

      {/* Resumo de saldos */}
      <section className="cards-resumo">
        <div className="card-resumo">
          <span className="card-resumo-icone indigo">
            <IconeCarteira />
          </span>
          <div className="card-resumo-info">
            <div className="card-resumo-rotulo">Saldo total em contas conjuntas</div>
            <div className="card-resumo-valor">{moeda(saldoTotal)}</div>
            <div className="card-resumo-tendencia alta">Soma de todas as contas conjuntas</div>
          </div>
        </div>
      </section>

      <section>
        <CabecalhoSecao
          titulo="Suas Contas Conjuntas"
          acao="Nova Conta Conjunta"
          onAcao={() => setModalNovaConta(true)}
          acaoSecundaria="Contas Conjuntas"
          onAcaoSecundaria={() => setModalGerenciarContas(true)}
        />
        {carregando ? (
          <div className="estado-vazio">Carregando contas conjuntas...</div>
        ) : erroBusca ? (
          <div className="estado-vazio erro">
            <p>{erroBusca}</p>
            <button type="button" className="btn btn-ghost" onClick={carregarContas}>
              Tentar novamente
            </button>
          </div>
        ) : contas.length === 0 ? (
          <div className="estado-vazio">Nenhuma conta conjunta cadastrada.</div>
        ) : (
          <div className="contas-grid">
            {contas.map((conta) => (
              <CardContaConjunta
                key={conta.id}
                conta={conta}
                ehCriador={ehCriador(conta)}
                onEditar={setContaEditar}
                onConvidar={setContaConvidar}
                onExcluir={setContaExcluir}
                onDetalhes={setContaDetalhes}
                onSair={setContaSair}
              />
            ))}
          </div>
        )}
      </section>

      <ModalNovaContaConjunta
        aberto={modalNovaConta}
        aoFechar={() => setModalNovaConta(false)}
        aoCriar={adicionarConta}
      />

      <ModalEditarContaConjunta
        conta={contaEditar}
        aoFechar={() => setContaEditar(null)}
        aoSalvar={atualizarConta}
      />

      <ModalExcluirContaConjunta
        conta={contaExcluir}
        aoFechar={() => setContaExcluir(null)}
        aoExcluir={removerConta}
      />

      <ModalDetalhesContaConjunta
        conta={contaDetalhes}
        membroId={membroId}
        aoFechar={() => setContaDetalhes(null)}
      />

      <ModalConvidarContaConjunta
        conta={contaConvidar}
        aoFechar={() => setContaConvidar(null)}
        aoAtualizar={(atualizada) => {
          atualizarConta(atualizada)
          setContaConvidar(atualizada)
        }}
      />

      <ModalSairContaConjunta
        conta={contaSair}
        aoFechar={() => setContaSair(null)}
        aoSair={removerConta}
      />

      <ModalGerenciarContasConjuntas
        aberto={modalGerenciarContas}
        aoFechar={() => setModalGerenciarContas(false)}
        contas={contas}
        ehCriador={ehCriador}
        aoSalvar={atualizarConta}
        aoExcluir={removerConta}
      />
    </div>
  )
}

/* ---------- Modal de criação de conta conjunta (POST) ---------- */

interface ModalNovaContaConjuntaProps {
  aberto: boolean
  aoFechar: () => void
  aoCriar: (conta: ContaConjuntaResponse) => void
}

function ModalNovaContaConjunta({ aberto, aoFechar, aoCriar }: ModalNovaContaConjuntaProps) {
  const [nome, setNome] = useState('')
  const [descricao, setDescricao] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  useEffect(() => {
    if (aberto) {
      setNome('')
      setDescricao('')
      setErro('')
    }
  }, [aberto])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErro('')
    setCarregando(true)
    try {
      const criada = await criarContaConjunta({
        nome: nome.trim(),
        descricao: descricao.trim() || null,
      })
      aoCriar(criada)
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível criar a conta conjunta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Nova Conta Conjunta"
      subtitulo="Crie uma conta para organizar as finanças compartilhadas da família."
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Nome da Conta
          <input
            type="text"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            placeholder="Ex: Casa, Viagem em família..."
            minLength={2}
            maxLength={100}
            required
            autoFocus
          />
        </label>

        <label className="campo">
          Descrição (opcional)
          <input
            type="text"
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
            placeholder="Ex: Despesas da casa divididas entre o casal"
            minLength={2}
            maxLength={100}
          />
        </label>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primario" disabled={carregando}>
            {carregando ? 'Criando...' : 'Criar Conta'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

/* ---------- Modal de edição de conta conjunta (PUT) ---------- */

interface ModalEditarContaConjuntaProps {
  conta: ContaConjuntaResponse | null
  aoFechar: () => void
  aoSalvar: (atualizada: ContaConjuntaResponse) => void
}

function ModalEditarContaConjunta({ conta, aoFechar, aoSalvar }: ModalEditarContaConjuntaProps) {
  const [nome, setNome] = useState('')
  const [descricao, setDescricao] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const aberto = conta !== null

  useEffect(() => {
    if (conta) {
      setNome(conta.nome)
      setDescricao(conta.descricao ?? '')
      setErro('')
    }
  }, [conta])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!conta) return
    setErro('')
    setCarregando(true)
    try {
      // Descrição vazia é enviada como "" para limpar o campo no backend
      const atualizada = await editarContaConjunta(conta.id, {
        nome: nome.trim(),
        descricao: descricao.trim(),
      })
      aoSalvar({ ...conta, ...atualizada })
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível editar a conta conjunta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Editar Conta Conjunta"
      subtitulo="Atualize o nome e a descrição da conta."
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Nome da Conta
          <input
            type="text"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            placeholder="Ex: Casa, Viagem em família..."
            minLength={2}
            maxLength={100}
            required
            autoFocus
          />
        </label>

        <label className="campo">
          Descrição (opcional)
          <input
            type="text"
            value={descricao}
            onChange={(e) => setDescricao(e.target.value)}
            placeholder="Ex: Despesas da casa divididas entre o casal"
            maxLength={100}
          />
        </label>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primario" disabled={carregando}>
            {carregando ? 'Salvando...' : 'Salvar Alterações'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

/* ---------- Modal de exclusão de conta conjunta (DELETE) ---------- */

interface ModalExcluirContaConjuntaProps {
  conta: ContaConjuntaResponse | null
  aoFechar: () => void
  aoExcluir: (id: number) => void
}

function ModalExcluirContaConjunta({ conta, aoFechar, aoExcluir }: ModalExcluirContaConjuntaProps) {
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const aberto = conta !== null

  useEffect(() => {
    if (conta) setErro('')
  }, [conta])

  const handleConfirmar = async () => {
    if (!conta) return
    setErro('')
    setCarregando(true)
    try {
      await excluirContaConjunta(conta.id)
      aoExcluir(conta.id)
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível excluir a conta conjunta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Excluir Conta Conjunta"
      subtitulo="Esta ação não pode ser desfeita."
      aoFechar={aoFechar}
    >
      <div className="formulario">
        <p>
          Tem certeza que deseja excluir a conta conjunta <strong>{conta?.nome}</strong>?
        </p>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="button" className="btn btn-perigo" disabled={carregando} onClick={handleConfirmar}>
            {carregando ? 'Excluindo...' : 'Excluir'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

/* ---------- Modal para sair da conta conjunta (POST .../sair) ---------- */

interface ModalSairContaConjuntaProps {
  conta: ContaConjuntaResponse | null
  aoFechar: () => void
  aoSair: (id: number) => void
}

// Só quem não é o criador abre este modal (o criador exclui a conta em vez de sair).
function ModalSairContaConjunta({ conta, aoFechar, aoSair }: ModalSairContaConjuntaProps) {
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const aberto = conta !== null

  useEffect(() => {
    if (conta) setErro('')
  }, [conta])

  const handleConfirmar = async () => {
    if (!conta) return
    setErro('')
    setCarregando(true)
    try {
      await sairContaConjunta(conta.id)
      aoSair(conta.id)
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível sair da conta conjunta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Sair da Conta Conjunta"
      subtitulo="Para voltar, você precisará de um novo convite do criador."
      aoFechar={aoFechar}
    >
      <div className="formulario">
        <p>
          Tem certeza que deseja sair da conta conjunta <strong>{conta?.nome}</strong>? As
          transações que você lançou continuam no histórico da conta.
        </p>

        {erro && <p className="erro">{erro}</p>}

        <div className="formulario-acoes">
          <button type="button" className="btn btn-ghost" onClick={aoFechar}>
            Cancelar
          </button>
          <button type="button" className="btn btn-perigo" disabled={carregando} onClick={handleConfirmar}>
            {carregando ? 'Saindo...' : 'Sair'}
          </button>
        </div>
      </div>
    </Modal>
  )
}

/* ---------- Modal de detalhes (participantes + histórico de transações) ---------- */

interface ModalDetalhesContaConjuntaProps {
  conta: ContaConjuntaResponse | null
  membroId: number | null
  aoFechar: () => void
}

function ModalDetalhesContaConjunta({ conta, membroId, aoFechar }: ModalDetalhesContaConjuntaProps) {
  const [transacoes, setTransacoes] = useState<TransacaoResponse[]>([])
  const [carregando, setCarregando] = useState(false)
  const [erro, setErro] = useState('')

  const aberto = conta !== null

  useEffect(() => {
    if (!conta) return
    let cancelado = false
    setTransacoes([])
    setErro('')
    setCarregando(true)
    ;(async () => {
      try {
        const data = await listarTransacoes({ contaConjuntaId: conta.id })
        if (!cancelado) setTransacoes(data)
      } catch (err: unknown) {
        if (!cancelado) setErro(extrairErro(err, 'Não foi possível carregar o histórico.'))
      } finally {
        if (!cancelado) setCarregando(false)
      }
    })()
    return () => {
      cancelado = true
    }
  }, [conta])

  return (
    <Modal
      aberto={aberto}
      titulo="Detalhes da Conta Conjunta"
      subtitulo={`Participantes e histórico de transações — ${conta?.nome ?? ''}`}
      aoFechar={aoFechar}
      largura="lg"
    >
      {conta?.descricao && <p className="conjunta-descricao">{conta.descricao}</p>}

      <span className="campo-rotulo">Participantes</span>
      <div className="conjunta-participantes">
        {(conta?.membros ?? []).map((m) => (
          <span key={m.id} className={`tag ${m.id === conta?.criadorId ? 'verde' : 'fosco'}`}>
            {m.nome}
            {m.id === membroId ? ' (você)' : ''}
            {m.id === conta?.criadorId ? ' · criador' : ''}
          </span>
        ))}
      </div>

      <span className="campo-rotulo">Histórico de transações</span>
      <div className="historico-transacoes">
        {carregando ? (
          <div className="estado-vazio">Carregando transações...</div>
        ) : erro ? (
          <div className="estado-vazio erro">
            <p>{erro}</p>
          </div>
        ) : transacoes.length === 0 ? (
          <div className="estado-vazio">Nenhuma transação encontrada.</div>
        ) : (
          <ul className="historico-lista">
            {transacoes.map((t) => {
              const entrada = t.tipo === TipoTransacao.ENTRADA
              return (
                <li key={t.id} className="historico-item">
                  <div className="historico-item-desc">
                    <div className="historico-item-nome">{t.descricao || 'Sem descrição'}</div>
                    <div className="historico-item-data">{formatarData(t.dataTransacao)}</div>
                  </div>
                  <div className={`historico-item-valor ${entrada ? 'entrada' : 'saida'}`}>
                    {entrada ? '+' : '-'}{moeda(t.valor)}
                  </div>
                </li>
              )
            })}
          </ul>
        )}
      </div>
    </Modal>
  )
}

/* ---------- Modal de convite (POST/GET/DELETE em api/Convite) ---------- */

interface ModalConvidarContaConjuntaProps {
  conta: ContaConjuntaResponse | null
  aoFechar: () => void
  aoAtualizar: (atualizada: ContaConjuntaResponse) => void
}

// Só o criador abre este modal: convida por nome de usuário ou e-mail,
// acompanha/cancela os convites ainda pendentes e remove participantes.
function ModalConvidarContaConjunta({ conta, aoFechar, aoAtualizar }: ModalConvidarContaConjuntaProps) {
  const [usuarioOuEmail, setUsuarioOuEmail] = useState('')
  const [convites, setConvites] = useState<ConviteResponse[]>([])
  const [carregando, setCarregando] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [cancelandoId, setCancelandoId] = useState<number | null>(null)
  const [removendoId, setRemovendoId] = useState<number | null>(null)
  const [removendoBtn, setRemovendoBtn] = useState(false)
  const [erro, setErro] = useState('')

  const aberto = conta !== null
  const contaId = conta?.id

  // Ao abrir: carrega os convites pendentes da conta
  // (depende só do id para não recarregar quando a lista de participantes muda)
  useEffect(() => {
    if (contaId === undefined) return
    let cancelado = false
    setUsuarioOuEmail('')
    setErro('')
    setConvites([])
    setRemovendoId(null)
    setCarregando(true)
    listarConvitesDaConta(contaId)
      .then((data) => {
        if (!cancelado) setConvites(data)
      })
      .catch((err: unknown) => {
        if (!cancelado) setErro(extrairErro(err, 'Não foi possível carregar os convites.'))
      })
      .finally(() => {
        if (!cancelado) setCarregando(false)
      })
    return () => {
      cancelado = true
    }
  }, [contaId])

  // POST — envia o convite
  const handleConvidar = async (e: FormEvent) => {
    e.preventDefault()
    if (!conta) return
    setErro('')
    setEnviando(true)
    try {
      const criado = await criarConvite({
        contaConjuntaId: conta.id,
        usuarioOuEmail: usuarioOuEmail.trim(),
      })
      setConvites((prev) => [criado, ...prev])
      setUsuarioOuEmail('')
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível enviar o convite.'))
    } finally {
      setEnviando(false)
    }
  }

  // DELETE — cancela um convite pendente
  const handleCancelar = async (id: number) => {
    setErro('')
    setCancelandoId(id)
    try {
      await cancelarConvite(id)
      setConvites((prev) => prev.filter((c) => c.id !== id))
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível cancelar o convite.'))
    } finally {
      setCancelandoId(null)
    }
  }

  // DELETE — remove um participante (após confirmação na própria linha)
  const confirmarRemocao = async () => {
    if (!conta || removendoId === null) return
    setErro('')
    setRemovendoBtn(true)
    try {
      const atualizada = await removerParticipante(conta.id, removendoId)
      aoAtualizar(atualizada)
      setRemovendoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível remover o participante.'))
    } finally {
      setRemovendoBtn(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Convidar para a Conta Conjunta"
      subtitulo={`Convide alguém para "${conta?.nome ?? ''}" pelo nome de usuário ou e-mail.`}
      aoFechar={aoFechar}
      largura="lg"
    >
      <div className="gerenciar-sub">
        {/* Formulário de convite (POST) */}
        <form className="formulario" onSubmit={handleConvidar}>
          <div className="gerenciar-sub-nova conjunta-convite-form">
            <label className="campo">
              Usuário ou e-mail
              <input
                type="text"
                value={usuarioOuEmail}
                onChange={(e) => setUsuarioOuEmail(e.target.value)}
                placeholder="Ex: maria ou maria@email.com"
                maxLength={100}
                required
                autoFocus={aberto}
              />
            </label>
            <button type="submit" className="btn btn-primario" disabled={enviando}>
              {enviando ? 'Enviando...' : 'Convidar'}
            </button>
          </div>
        </form>

        {erro && <p className="erro">{erro}</p>}

        {/* Participantes — o criador pode remover os demais */}
        <span className="campo-rotulo">Participantes</span>
        <div className="gerenciar-sub-lista">
          {(conta?.membros ?? []).map((m) => (
            <div key={m.id} className="gerenciar-sub-item">
              <div className="gerenciar-sub-info">
                <span className="gerenciar-sub-nome">{m.nome}</span>
                <span className="gerenciar-sub-conta">@{m.nomeDeUsuario}</span>
              </div>
              <div className="gerenciar-sub-acoes">
                {m.id === conta?.criadorId ? (
                  <span className="tag verde">Criador</span>
                ) : (
                  <button
                    type="button"
                    className="gerenciar-sub-icone perigo"
                    aria-label="Remover participante"
                    title="Remover participante"
                    onClick={() => setRemovendoId((prev) => (prev === m.id ? null : m.id))}
                  >
                    <IconeLixeira />
                  </button>
                )}
              </div>

              {removendoId === m.id && (
                <div className="gerenciar-sub-confirma">
                  <div className="gerenciar-sub-confirma-texto">
                    <span>Remover {m.nome} da conta?</span>
                  </div>
                  <div className="gerenciar-sub-confirma-acoes">
                    <button
                      type="button"
                      className="btn btn-perigo"
                      onClick={confirmarRemocao}
                      disabled={removendoBtn}
                    >
                      {removendoBtn ? 'Removendo...' : 'Remover'}
                    </button>
                    <button
                      type="button"
                      className="btn btn-ghost"
                      onClick={() => setRemovendoId(null)}
                    >
                      Cancelar
                    </button>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>

        {/* Convites pendentes (GET) */}
        <span className="campo-rotulo">Convites pendentes</span>
        <div className="gerenciar-sub-lista">
          {carregando ? (
            <div className="estado-vazio">Carregando convites...</div>
          ) : convites.length === 0 ? (
            <div className="estado-vazio">Nenhum convite pendente.</div>
          ) : (
            convites.map((convite) => (
              <div key={convite.id} className="gerenciar-sub-item">
                <div className="gerenciar-sub-info">
                  <span className="gerenciar-sub-nome">{convite.convidadoNome}</span>
                  <span className="gerenciar-sub-conta">
                    @{convite.convidadoNomeDeUsuario} · enviado em{' '}
                    {new Date(convite.criadoEm).toLocaleDateString('pt-BR')}
                  </span>
                </div>
                <div className="gerenciar-sub-acoes">
                  <span className="tag ambar">Pendente</span>
                  <button
                    type="button"
                    className="gerenciar-sub-icone perigo"
                    aria-label="Cancelar convite"
                    title="Cancelar convite"
                    disabled={cancelandoId === convite.id}
                    onClick={() => handleCancelar(convite.id)}
                  >
                    <IconeLixeira />
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </Modal>
  )
}

/* ---------- Modal de gerenciamento de contas conjuntas (PUT/DELETE) ---------- */

interface ModalGerenciarContasConjuntasProps {
  aberto: boolean
  aoFechar: () => void
  contas: ContaConjuntaResponse[]
  ehCriador: (conta: ContaConjuntaResponse) => boolean
  aoSalvar: (atualizada: ContaConjuntaResponse) => void
  aoExcluir: (id: number) => void
}

function ModalGerenciarContasConjuntas({
  aberto,
  aoFechar,
  contas,
  ehCriador,
  aoSalvar,
  aoExcluir,
}: ModalGerenciarContasConjuntasProps) {
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [editandoNome, setEditandoNome] = useState('')
  const [editandoDescricao, setEditandoDescricao] = useState('')
  const [editandoBtn, setEditandoBtn] = useState(false)
  const [excluindoId, setExcluindoId] = useState<number | null>(null)
  const [excluindoBtn, setExcluindoBtn] = useState(false)
  const [erro, setErro] = useState('')

  useEffect(() => {
    if (aberto) {
      setEditandoId(null)
      setExcluindoId(null)
      setErro('')
    }
  }, [aberto])

  const iniciarEdicao = (conta: ContaConjuntaResponse) => {
    setExcluindoId(null)
    setEditandoId(conta.id)
    setEditandoNome(conta.nome)
    setEditandoDescricao(conta.descricao ?? '')
    setErro('')
  }

  const pedirExclusao = (id: number) => {
    setEditandoId(null)
    setExcluindoId((prev) => (prev === id ? null : id))
    setErro('')
  }

  const confirmarExclusao = async () => {
    if (excluindoId === null) return
    setErro('')
    setExcluindoBtn(true)
    try {
      await excluirContaConjunta(excluindoId)
      aoExcluir(excluindoId)
      setExcluindoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível excluir a conta conjunta.'))
    } finally {
      setExcluindoBtn(false)
    }
  }

  const salvarEdicao = async () => {
    if (editandoId === null) return
    const original = contas.find((c) => c.id === editandoId)
    setErro('')
    setEditandoBtn(true)
    try {
      // Descrição vazia é enviada como "" para limpar o campo no backend
      const atualizada = await editarContaConjunta(editandoId, {
        nome: editandoNome.trim(),
        descricao: editandoDescricao.trim(),
      })
      aoSalvar(original ? { ...original, ...atualizada } : atualizada)
      setEditandoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível editar a conta conjunta.'))
    } finally {
      setEditandoBtn(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Editar Contas Conjuntas"
      subtitulo="Edite o nome e a descrição ou exclua as contas conjuntas que você criou."
      aoFechar={aoFechar}
      largura="lg"
    >
      <div className="gerenciar-sub">
        {erro && <p className="erro">{erro}</p>}

        <div className="gerenciar-sub-lista">
          {contas.length === 0 ? (
            <div className="estado-vazio">Nenhuma conta conjunta cadastrada.</div>
          ) : (
            contas.map((conta) => (
              <div key={conta.id} className="gerenciar-sub-item">
                {editandoId === conta.id ? (
                  <div className="gerenciar-meta-editar">
                    <input
                      type="text"
                      className="nome"
                      value={editandoNome}
                      onChange={(e) => setEditandoNome(e.target.value)}
                      placeholder="Nome da conta"
                      aria-label="Nome da conta"
                      minLength={2}
                      maxLength={100}
                      autoFocus
                    />
                    <input
                      type="text"
                      className="nome"
                      value={editandoDescricao}
                      onChange={(e) => setEditandoDescricao(e.target.value)}
                      placeholder="Descrição (opcional)"
                      aria-label="Descrição"
                      maxLength={100}
                    />
                    <div className="gerenciar-meta-editar-acoes">
                      <button
                        type="button"
                        className="btn btn-primario"
                        onClick={salvarEdicao}
                        disabled={editandoBtn || !editandoNome.trim()}
                      >
                        {editandoBtn ? 'Salvando...' : 'Salvar'}
                      </button>
                      <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={() => setEditandoId(null)}
                      >
                        Cancelar
                      </button>
                    </div>
                  </div>
                ) : (
                  <>
                    <div className="gerenciar-sub-info">
                      <span className="gerenciar-sub-nome">{conta.nome}</span>
                      {conta.descricao && (
                        <span className="gerenciar-sub-conta">{conta.descricao}</span>
                      )}
                      <span className="gerenciar-sub-conta">
                        {moeda(conta.saldo)} · {quantidadeParticipantes(conta)}
                      </span>
                    </div>
                    <div className="gerenciar-sub-acoes">
                      {ehCriador(conta) ? (
                        <>
                          <button
                            type="button"
                            className="gerenciar-sub-icone"
                            aria-label="Editar"
                            title="Editar"
                            onClick={() => iniciarEdicao(conta)}
                          >
                            <IconeLapis />
                          </button>
                          <button
                            type="button"
                            className="gerenciar-sub-icone perigo"
                            aria-label="Excluir"
                            title="Excluir"
                            onClick={() => pedirExclusao(conta.id)}
                          >
                            <IconeLixeira />
                          </button>
                        </>
                      ) : (
                        <span className="tag fosco">Compartilhada</span>
                      )}
                    </div>
                  </>
                )}

                {excluindoId === conta.id && (
                  <div className="gerenciar-sub-confirma">
                    <div className="gerenciar-sub-confirma-texto">
                      <span>Excluir "{conta.nome}"?</span>
                    </div>
                    <div className="gerenciar-sub-confirma-acoes">
                      <button
                        type="button"
                        className="btn btn-perigo"
                        onClick={confirmarExclusao}
                        disabled={excluindoBtn}
                      >
                        {excluindoBtn ? 'Excluindo...' : 'Excluir'}
                      </button>
                      <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={() => setExcluindoId(null)}
                      >
                        Cancelar
                      </button>
                    </div>
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </div>
    </Modal>
  )
}
