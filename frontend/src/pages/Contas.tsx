/**
 * Contas — Seção de Contas Bancárias e Cartões de Crédito.
 * Contas bancárias são sincronizadas com o backend (GET + POST).
 * Cartões de crédito permanecem como mock (o backend não possui esse modelo).
 */

import { useState, useEffect, useCallback, useRef, type FormEvent } from 'react'
import {
  IconeBanco,
  IconeCartaoCredito,
  IconeEllipsisVertical,
  IconeMais,
  IconeCarteira,
  IconeSetaCima,
  IconeSetaBaixo,
  IconeLapis,
  IconeLixeira,
} from '../components/icons'
import Modal from '../components/Modal'
import {
  listarContas,
  criarConta,
  editarConta,
  excluirConta,
  alterarContaPrincipal,
} from '../services/contas'
import { listarTransacoes } from '../services/transacoes'
import type { ContaResponse, CriarContaRequest } from '../types/conta'
import { StatusTransacao, TipoTransacao } from '../types/transacao'
import type { TransacaoResponse } from '../types/transacao'
import { extrairErro } from '../utils/erros'
/* ---------- Tipos e dados (mock — cartões de crédito) ---------- */

interface CreditCard {
  id: number
  cardName: string
  issuerName: string
  cardNumberLast4: string
  creditLimit: number
  currentBalance: number
  dueDate: number
}

const CARTOES: CreditCard[] = [
  { id: 1, cardName: 'Nubank - Mastercard Gold', issuerName: 'Nubank', cardNumberLast4: '4521', creditLimit: 6000, currentBalance: 1150.2, dueDate: 15 },
  { id: 2, cardName: 'Itaú - Visa Infinite', issuerName: 'Itaú', cardNumberLast4: '8873', creditLimit: 12000, currentBalance: 3200.0, dueDate: 28 },
]

/* ---------- Helpers ---------- */

function moeda(valor: number) {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

/* ---------- Menu de opções (três pontos) ---------- */

interface OpcoesProps {
  onEditar?: () => void
  onDetalhes?: () => void
  onExcluir?: () => void
}

function MenuOpcoes({ onEditar, onDetalhes, onExcluir }: OpcoesProps) {
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
          <button type="button" className="conta-menu-item" onClick={() => { setAberto(false); onEditar?.() }}>
            Editar
          </button>
          <button type="button" className="conta-menu-item" onClick={() => { setAberto(false); onDetalhes?.() }}>
            Ver Detalhes
          </button>
          <button type="button" className="conta-menu-item" onClick={() => { setAberto(false); onExcluir?.() }}>
            Excluir
          </button>
        </div>
      )}
    </div>
  )
}

/* ---------- Cartão de Conta Bancária ---------- */

function CardContaBancaria({
  conta,
  onEditar,
  onExcluir,
  onDetalhes,
}: {
  conta: ContaResponse
  onEditar: (conta: ContaResponse) => void
  onExcluir: (conta: ContaResponse) => void
  onDetalhes: (conta: ContaResponse) => void
}) {
  const { nome, saldo, saldoPrevisto, saldoInicialDefinido, contaPrincipal } = conta
  const [totais, setTotais] = useState<{ entrada: number; despesa: number }>({
    entrada: 0,
    despesa: 0,
  })

  useEffect(() => {
    let ativo = true
    listarTransacoes({ contaId: conta.id })
      .then((transacoes) => {
        if (!ativo) return
        // Só as pagas/recebidas, como o saldo: saldo inicial + entradas − saídas fecha com o card
        const concluidas = transacoes.filter((t) => t.status === StatusTransacao.CONCLUIDA)
        const entrada = concluidas
          .filter((t) => t.tipo === TipoTransacao.ENTRADA)
          .reduce((acc, t) => acc + t.valor, 0)
        const despesa = concluidas
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
        <span className="conta-card-icone banco">
          <IconeBanco />
        </span>
        <MenuOpcoes
          onEditar={() => onEditar(conta)}
          onDetalhes={() => onDetalhes(conta)}
          onExcluir={() => onExcluir(conta)}
        />
      </div>

      <div className="conta-card-nome">{nome}</div>

      <div className="conta-card-saldo">{moeda(saldo)}</div>
      <div className="conta-card-previsto">
        {saldoInicialDefinido
          ? `Previsto: ${moeda(saldoPrevisto ?? saldo)}`
          : 'Aguardando o saldo inicial (no Painel)'}
      </div>

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
        <span className={`tag ${contaPrincipal ? 'verde' : 'fosco'}`}>
          {contaPrincipal ? 'Conta Principal' : 'Conta Secundária'}
        </span>
      </div>
    </div>
  )
}

/* ---------- Cartão de Cartão de Crédito ---------- */

function CardCartaoCredito({ cartao }: { cartao: CreditCard }) {
  const { cardName, issuerName, cardNumberLast4, creditLimit, currentBalance, dueDate } = cartao
  const usoPct = Math.min((currentBalance / creditLimit) * 100, 100)
  const limiteDisponivel = creditLimit - currentBalance

  return (
    <div className="conta-card">
      <div className="conta-card-topo">
        <span className="conta-card-icone cartao">
          <IconeCartaoCredito />
        </span>
        <MenuOpcoes />
      </div>

      <div className="conta-card-nome">{cardName}</div>
      <div className="conta-card-sub">•••• {cardNumberLast4}</div>

      <div className="cartao-linha">
        <span>Limite: {moeda(limiteDisponivel)}</span>
      </div>
      <div className="cartao-linha">
        <span>Fatura: <strong>{moeda(currentBalance)}</strong></span>
      </div>

      {/* Barra de progresso de uso do limite */}
      <div className="cartao-progresso">
        <div className="cartao-progresso-info">
          <span>Uso do limite</span>
          <span>{usoPct.toFixed(0)}%</span>
        </div>
        <div className="cartao-progresso-bar">
          <div
            className={`cartao-progresso-fill ${usoPct > 70 ? 'alto' : ''}`}
            style={{ width: `${usoPct}%` }}
          />
        </div>
      </div>

      <div className="conta-card-info">
        <span className="tag fosco">Vence dia {dueDate}</span>
        <span className="conta-card-update">{issuerName}</span>
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

export default function Contas() {
  const [contas, setContas] = useState<ContaResponse[]>([])
  const [carregando, setCarregando] = useState(true)
  const [erroBusca, setErroBusca] = useState('')
  const [modalNovaConta, setModalNovaConta] = useState(false)
  const [modalGerenciarContas, setModalGerenciarContas] = useState(false)
  const [contaEditar, setContaEditar] = useState<ContaResponse | null>(null)
  const [contaExcluir, setContaExcluir] = useState<ContaResponse | null>(null)
  const [contaDetalhes, setContaDetalhes] = useState<ContaResponse | null>(null)

  const carregarContas = useCallback(async () => {
    setCarregando(true)
    setErroBusca('')
    try {
      const data = await listarContas()
      setContas(data)
    } catch (err: unknown) {
      setErroBusca(extrairErro(err, 'Não foi possível carregar as contas.'))
    } finally {
      setCarregando(false)
    }
  }, [])

  useEffect(() => {
    carregarContas()
  }, [carregarContas])

  const adicionarConta = (conta: ContaResponse) => {
    setContas((prev) => [...prev, conta])
  }

  const atualizarConta = (atualizada: ContaResponse) => {
    setContas((prev) => prev.map((c) => (c.id === atualizada.id ? atualizada : c)))
  }

  const removerConta = (id: number) => {
    setContas((prev) => prev.filter((c) => c.id !== id))
  }

  const alterarPrincipal = (nova: ContaResponse) => {
    setContas((prev) => prev.map((c) => ({ ...c, contaPrincipal: c.id === nova.id })))
  }

  const saldoTotal = contas.reduce((acc, c) => acc + c.saldo, 0)

  return (
    <div className="pagina">
      <div className="pagina-cabecalho">
        <div>
          <h1 className="pagina-titulo">Contas</h1>
          <p className="pagina-subtitulo">Gerencie suas contas bancárias e cartões de crédito.</p>
        </div>
      </div>

      {/* Resumo de saldos */}
      <section className="cards-resumo">
        <div className="card-resumo">
          <span className="card-resumo-icone indigo">
            <IconeCarteira />
          </span>
          <div className="card-resumo-info">
            <div className="card-resumo-rotulo">Saldo total em contas</div>
            <div className="card-resumo-valor">{moeda(saldoTotal)}</div>
            <div className="card-resumo-tendencia alta">Soma de todas as contas bancárias</div>
          </div>
        </div>
      </section>

      {/* Área A: Contas bancárias */}
      <section>
        <CabecalhoSecao
          titulo="Suas Contas Bancárias"
          acao="Nova Conta"
          onAcao={() => setModalNovaConta(true)}
          acaoSecundaria="Contas"
          onAcaoSecundaria={() => setModalGerenciarContas(true)}
        />
        {carregando ? (
          <div className="estado-vazio">Carregando contas...</div>
        ) : erroBusca ? (
          <div className="estado-vazio erro">
            <p>{erroBusca}</p>
            <button type="button" className="btn btn-ghost" onClick={carregarContas}>
              Tentar novamente
            </button>
          </div>
        ) : contas.length === 0 ? (
          <div className="estado-vazio">Nenhuma conta cadastrada.</div>
        ) : (
          <div className="contas-grid">
            {contas.map((conta) => (
              <CardContaBancaria
                key={conta.id}
                conta={conta}
                onEditar={setContaEditar}
                onExcluir={setContaExcluir}
                onDetalhes={setContaDetalhes}
              />
            ))}
          </div>
        )}
      </section>

      {/* Área B: Cartões de crédito */}
      <section>
        <CabecalhoSecao titulo="Seus Cartões de Crédito" acao="+ Novo Cartão" />
        <div className="contas-grid">
          {CARTOES.map((cartao) => (
            <CardCartaoCredito key={cartao.id} cartao={cartao} />
          ))}
        </div>
      </section>

      <ModalNovaConta
        aberto={modalNovaConta}
        aoFechar={() => setModalNovaConta(false)}
        aoCriar={adicionarConta}
      />

      <ModalEditarConta
        conta={contaEditar}
        aoFechar={() => setContaEditar(null)}
        aoSalvar={atualizarConta}
      />

      <ModalExcluirConta
        conta={contaExcluir}
        aoFechar={() => setContaExcluir(null)}
        aoExcluir={removerConta}
      />

      <ModalDetalhesConta
        conta={contaDetalhes}
        aoFechar={() => setContaDetalhes(null)}
      />

      <ModalGerenciarContas
        aberto={modalGerenciarContas}
        aoFechar={() => setModalGerenciarContas(false)}
        contas={contas}
        aoSalvar={atualizarConta}
        aoExcluir={removerConta}
        aoAlterarPrincipal={alterarPrincipal}
      />
    </div>
  )
}

/* ---------- Modal de criação de nova conta (POST) ---------- */

interface ModalNovaContaProps {
  aberto: boolean
  aoFechar: () => void
  aoCriar: (conta: ContaResponse) => void
}

function ModalNovaConta({ aberto, aoFechar, aoCriar }: ModalNovaContaProps) {
  const [nome, setNome] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  useEffect(() => {
    if (aberto) {
      setNome('')
      setErro('')
    }
  }, [aberto])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setErro('')
    setCarregando(true)
    try {
      // Toda conta nova nasce secundária: o membro já tem uma conta principal desde o
      // cadastro. Para trocar a principal, use "Alterar conta principal" no modal de Contas.
      const request: CriarContaRequest = { nome: nome.trim(), contaPrincipal: false }
      const criada = await criarConta(request)
      aoCriar(criada)
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível criar a conta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Nova Conta"
      subtitulo="Cadastre uma nova conta bancária para organizar suas finanças."
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Nome da Conta
          <input
            type="text"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            placeholder="Ex: Banco do Brasil, Nubank..."
            minLength={2}
            maxLength={100}
            required
            autoFocus
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

/* ---------- Modal de edição de conta (PUT) ---------- */

interface ModalEditarContaProps {
  conta: ContaResponse | null
  aoFechar: () => void
  aoSalvar: (atualizada: ContaResponse) => void
}

function ModalEditarConta({ conta, aoFechar, aoSalvar }: ModalEditarContaProps) {
  const [nome, setNome] = useState('')
  const [erro, setErro] = useState('')
  const [carregando, setCarregando] = useState(false)

  const aberto = conta !== null

  useEffect(() => {
    if (conta) {
      setNome(conta.nome)
      setErro('')
    }
  }, [conta])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!conta) return
    setErro('')
    setCarregando(true)
    try {
      const atualizada = await editarConta(conta.id, { nome: nome.trim() })
      aoSalvar({ ...conta, ...atualizada, nome: atualizada.nome })
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível editar a conta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Editar Conta"
      subtitulo="Atualize o nome da conta."
      aoFechar={aoFechar}
    >
      <form className="formulario" onSubmit={handleSubmit}>
        <label className="campo">
          Nome da Conta
          <input
            type="text"
            value={nome}
            onChange={(e) => setNome(e.target.value)}
            placeholder="Ex: Banco do Brasil, Nubank..."
            minLength={2}
            maxLength={100}
            required
            autoFocus
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

/* ---------- Modal de gerenciamento de contas (PUT) ---------- */

interface ModalGerenciarContasProps {
  aberto: boolean
  aoFechar: () => void
  contas: ContaResponse[]
  aoSalvar: (atualizada: ContaResponse) => void
  aoExcluir: (id: number) => void
  aoAlterarPrincipal: (novaPrincipal: ContaResponse) => void
}

function ModalGerenciarContas({
  aberto,
  aoFechar,
  contas,
  aoSalvar,
  aoExcluir,
  aoAlterarPrincipal,
}: ModalGerenciarContasProps) {
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [editandoNome, setEditandoNome] = useState('')
  const [editandoBtn, setEditandoBtn] = useState(false)
  const [excluindoId, setExcluindoId] = useState<number | null>(null)
  const [excluindoBtn, setExcluindoBtn] = useState(false)
  const [alterandoPrincipal, setAlterandoPrincipal] = useState(false)
  const [novaPrincipalId, setNovaPrincipalId] = useState<number | null>(null)
  const [alterandoBtn, setAlterandoBtn] = useState(false)
  const [erro, setErro] = useState('')

  useEffect(() => {
    if (aberto) {
      setEditandoId(null)
      setExcluindoId(null)
      setAlterandoPrincipal(false)
      setNovaPrincipalId(null)
      setErro('')
    }
  }, [aberto])

  const iniciarEdicao = (conta: ContaResponse) => {
    setExcluindoId(null)
    setEditandoId(conta.id)
    setEditandoNome(conta.nome)
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
      await excluirConta(excluindoId)
      aoExcluir(excluindoId)
      setExcluindoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível excluir a conta.'))
    } finally {
      setExcluindoBtn(false)
    }
  }

  const pedirAlterarPrincipal = (id: number) => {
    setEditandoId(null)
    setExcluindoId(null)
    setNovaPrincipalId((prev) => (prev === id ? null : id))
    setErro('')
  }

  const confirmarAlterarPrincipal = async () => {
    if (novaPrincipalId === null) return
    setErro('')
    setAlterandoBtn(true)
    try {
      const atualizada = await alterarContaPrincipal(novaPrincipalId)
      aoAlterarPrincipal(atualizada)
      setNovaPrincipalId(null)
      setAlterandoPrincipal(false)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível alterar a conta principal.'))
    } finally {
      setAlterandoBtn(false)
    }
  }

  const salvarEdicao = async () => {
    if (editandoId === null) return
    setErro('')
    setEditandoBtn(true)
    try {
      const atualizada = await editarConta(editandoId, { nome: editandoNome.trim() })
      aoSalvar(atualizada)
      setEditandoId(null)
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível editar a conta.'))
    } finally {
      setEditandoBtn(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Editar Contas"
      subtitulo="Renomeie suas contas bancárias."
      aoFechar={aoFechar}
      largura="lg"
    >
      <div className="gerenciar-sub">
        {erro && <p className="erro">{erro}</p>}

        <button
          type="button"
          className="btn btn-primario"
          onClick={() => {
            setNovaPrincipalId(null)
            setAlterandoPrincipal((v) => !v)
          }}
        >
          <IconeCarteira />
          <span>Alterar conta principal</span>
        </button>

        {alterandoPrincipal && (
          <div className="gerenciar-sub-lista">
            {contas.filter((c) => !c.contaPrincipal).length === 0 ? (
              <div className="estado-vazio">Nenhuma outra conta disponível.</div>
            ) : (
              contas
                .filter((c) => !c.contaPrincipal)
                .map((conta) => (
                  <div key={conta.id} className="gerenciar-sub-item">
                    <div className="gerenciar-sub-info">
                      <span className="gerenciar-sub-nome">{conta.nome}</span>
                      <span className="gerenciar-sub-conta">{moeda(conta.saldo)}</span>
                    </div>
                    <div className="gerenciar-sub-acoes">
                      <button
                        type="button"
                        className="btn btn-ghost"
                        onClick={() => pedirAlterarPrincipal(conta.id)}
                      >
                        Tornar principal
                      </button>
                    </div>

                    {novaPrincipalId === conta.id && (
                      <div className="gerenciar-sub-confirma">
                        <div className="gerenciar-sub-confirma-texto">
                          <span>Tornar "{conta.nome}" a conta principal?</span>
                        </div>
                        <div className="gerenciar-sub-confirma-acoes">
                          <button
                            type="button"
                            className="btn btn-primario"
                            onClick={confirmarAlterarPrincipal}
                            disabled={alterandoBtn}
                          >
                            {alterandoBtn ? 'Alterando...' : 'Confirmar'}
                          </button>
                          <button
                            type="button"
                            className="btn btn-ghost"
                            onClick={() => setNovaPrincipalId(null)}
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
        )}

        <div className="gerenciar-sub-lista">
          {contas.length === 0 ? (
            <div className="estado-vazio">Nenhuma conta cadastrada.</div>
          ) : (
            contas.map((conta) => (
              <div key={conta.id} className="gerenciar-sub-item">
                {editandoId === conta.id ? (
                  <div className="gerenciar-sub-editar">
                    <input
                      type="text"
                      value={editandoNome}
                      onChange={(e) => setEditandoNome(e.target.value)}
                      minLength={2}
                      maxLength={100}
                      autoFocus
                    />
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
                ) : (
                  <>
                    <div className="gerenciar-sub-info">
                      <span className="gerenciar-sub-nome">{conta.nome}</span>
                      <span className="gerenciar-sub-conta">{moeda(conta.saldo)}</span>
                    </div>
                    <div className="gerenciar-sub-acoes">
                      <span className={`tag ${conta.contaPrincipal ? 'verde' : 'fosco'}`}>
                        {conta.contaPrincipal ? 'Principal' : 'Secundária'}
                      </span>
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

/* ---------- Modal de exclusão de conta (DELETE) ---------- */

interface ModalExcluirContaProps {
  conta: ContaResponse | null
  aoFechar: () => void
  aoExcluir: (id: number) => void
}

function ModalExcluirConta({ conta, aoFechar, aoExcluir }: ModalExcluirContaProps) {
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
      await excluirConta(conta.id)
      aoExcluir(conta.id)
      aoFechar()
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível excluir a conta.'))
    } finally {
      setCarregando(false)
    }
  }

  return (
    <Modal
      aberto={aberto}
      titulo="Excluir Conta"
      subtitulo="Esta ação não pode ser desfeita."
      aoFechar={aoFechar}
    >
      <div className="formulario">
        <p>
          Tem certeza que deseja excluir a conta <strong>{conta?.nome}</strong>?
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

/* ---------- Modal de detalhes da conta (histórico de transações) ---------- */

function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-')
  if (!ano || !mes || !dia) return data
  return `${dia}/${mes}/${ano}`
}

interface ModalDetalhesContaProps {
  conta: ContaResponse | null
  aoFechar: () => void
}

function ModalDetalhesConta({ conta, aoFechar }: ModalDetalhesContaProps) {
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
        const data = await listarTransacoes({ contaId: conta.id })
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
      titulo="Detalhes da Conta"
      subtitulo={`Histórico de transações — ${conta?.nome ?? ''}`}
      aoFechar={aoFechar}
      largura="lg"
    >
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
