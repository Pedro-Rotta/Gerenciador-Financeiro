/**
 * Configuracoes — tela de configurações da conta, aberta pelo menu do perfil.
 * Abas: Conta, Segurança, Notificações, Preferências e Privacidade (?aba=...).
 * Tudo é real: perfil e senha (api/Membro), sessões, preferências de notificação e de uso
 * (salvas no backend, cada mudança na hora), tema e notificações do navegador (neste
 * navegador), exportar os dados (planilha) e excluir a conta (com a senha).
 */

import { useEffect, useState, type ReactNode } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import {
  alterarSenha,
  editarPerfil,
  encerrarOutrasSessoes,
  excluirConta,
  obterPerfil,
  obterPreferenciasUso,
  salvarPreferenciasUso,
} from '../services/membros'
import { carregarOpcoesConta, type OpcaoConta } from '../services/opcoesConta'
import type { PreferenciasUso } from '../types/membro'
import { useTema, type ModoTema } from '../hooks/useTema'
import { extrairErro } from '../utils/erros'
import { baixarArquivo } from '../utils/download'
import { gerarExcelMeusDados, nomeArquivoMeusDados } from '../utils/excelMeusDados'
import { guardarPreferenciasUso, preferenciasUso } from '../utils/preferenciasUso'
import {
  desligarNotificacoesNavegador,
  ligarNotificacoesNavegador,
  navegadorSuportaNotificacoes,
  notificacoesNavegadorLigadas,
} from '../utils/notificacoesNavegador'
import {
  obterPreferenciasNotificacao,
  salvarPreferenciasNotificacao,
} from '../services/notificacoes'
import type { PreferenciasNotificacao } from '../types/notificacao'
import {
  IconeUsuario,
  IconeEscudo,
  IconeSino,
  IconeConfig,
  IconeLixeira,
} from '../components/icons'

type Aba = 'conta' | 'seguranca' | 'notificacoes' | 'preferencias' | 'privacidade'

const ABAS: { id: Aba; rotulo: string; icone: ReactNode }[] = [
  { id: 'conta', rotulo: 'Conta', icone: <IconeUsuario /> },
  { id: 'seguranca', rotulo: 'Segurança', icone: <IconeEscudo /> },
  { id: 'notificacoes', rotulo: 'Notificações', icone: <IconeSino /> },
  { id: 'preferencias', rotulo: 'Preferências', icone: <IconeConfig /> },
  { id: 'privacidade', rotulo: 'Privacidade e dados', icone: <IconeLixeira /> },
]

/* ---------- Peças da tela ---------- */

function Secao({ titulo, sub, children }: { titulo: string; sub?: string; children: ReactNode }) {
  return (
    <section className="painel config-secao">
      <div className="config-secao-cabecalho">
        <h2>{titulo}</h2>
        {sub && <p>{sub}</p>}
      </div>
      {children}
    </section>
  )
}

/** Interruptor liga/desliga. */
function Interruptor({
  ligado,
  aoAlternar,
  rotulo,
  desativado = false,
}: {
  ligado: boolean
  aoAlternar: () => void
  rotulo: string
  desativado?: boolean
}) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={ligado}
      aria-label={rotulo}
      disabled={desativado}
      className={`interruptor ${ligado ? 'ligado' : ''}`}
      onClick={aoAlternar}
    >
      <span className="interruptor-bolinha" />
    </button>
  )
}

/** Linha com título, descrição e o controle à direita. */
function Linha({ titulo, descricao, children }: { titulo: string; descricao?: string; children: ReactNode }) {
  return (
    <div className="config-linha">
      <div className="config-linha-texto">
        <span className="config-linha-titulo">{titulo}</span>
        {descricao && <span className="config-linha-desc">{descricao}</span>}
      </div>
      <div className="config-linha-controle">{children}</div>
    </div>
  )
}

/* ---------- Alterar senha (aba Conta, embaixo de Telefone) ---------- */

// Troca a senha conferindo a atual. As outras sessões caem; este navegador recebe um cookie novo e continua logado.
function AlterarSenha() {
  const { renovarSessao } = useAuth()
  const [senhaAtual, setSenhaAtual] = useState('')
  const [novaSenha, setNovaSenha] = useState('')
  const [confirmacaoSenha, setConfirmacaoSenha] = useState('')
  const [erro, setErro] = useState('')
  const [aviso, setAviso] = useState('')
  const [salvando, setSalvando] = useState(false)

  const salvar = async () => {
    setErro('')
    setAviso('')
    if (novaSenha.length < 6) {
      setErro('A senha deve ter entre 6 e 100 caracteres')
      return
    }
    if (novaSenha !== confirmacaoSenha) {
      setErro('As senhas não coincidem')
      return
    }
    setSalvando(true)
    try {
      const { expiraEm } = await alterarSenha({ senhaAtual, novaSenha, confirmacaoSenha })
      // O cookie novo vale mais 60 min: o aviso de sessão expirada passa a contar dele
      renovarSessao(expiraEm)
      setSenhaAtual('')
      setNovaSenha('')
      setConfirmacaoSenha('')
      setAviso('Senha alterada. As outras sessões abertas foram encerradas.')
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível alterar a senha.'))
    } finally {
      setSalvando(false)
    }
  }

  return (
    <div className="config-grade-inteira config-senha">
      <div className="config-perfil-nome">Alterar senha</div>
      <div className="config-grade">
        <label className="campo">
          Senha atual
          <input
            type="password"
            autoComplete="current-password"
            value={senhaAtual}
            onChange={(e) => setSenhaAtual(e.target.value)}
          />
        </label>
        <span />
        <label className="campo">
          Nova senha
          <input
            type="password"
            autoComplete="new-password"
            maxLength={100}
            value={novaSenha}
            onChange={(e) => setNovaSenha(e.target.value)}
          />
        </label>
        <label className="campo">
          Confirmar nova senha
          <input
            type="password"
            autoComplete="new-password"
            value={confirmacaoSenha}
            onChange={(e) => setConfirmacaoSenha(e.target.value)}
          />
        </label>
      </div>
      {erro && <p className="erro">{erro}</p>}
      {aviso && <p className="sucesso">{aviso}</p>}
      <div className="config-acoes">
        <button
          type="button"
          className="btn btn-primario"
          onClick={salvar}
          disabled={salvando || !senhaAtual || !novaSenha || !confirmacaoSenha}
        >
          {salvando ? 'Salvando...' : 'Atualizar senha'}
        </button>
      </div>
    </div>
  )
}

/* ---------- Abas ---------- */

// Perfil real: carrega de GET api/Membro e salva no PUT api/Membro
function AbaConta() {
  const { atualizarPerfil } = useAuth()
  const [nome, setNome] = useState('')
  const [nomeDeUsuario, setNomeDeUsuario] = useState('')
  const [email, setEmail] = useState('')
  const [telefone, setTelefone] = useState('')
  const [dataNascimento, setDataNascimento] = useState('')
  const [erro, setErro] = useState('')
  const [aviso, setAviso] = useState('')
  const [salvando, setSalvando] = useState(false)

  useEffect(() => {
    obterPerfil()
      .then((p) => {
        setNome(p.nome)
        setNomeDeUsuario(p.nomeDeUsuario)
        setEmail(p.email)
        setTelefone(p.telefone ?? '')
        setDataNascimento(p.dataNascimento ?? '')
      })
      .catch((err: unknown) => setErro(extrairErro(err, 'Não foi possível carregar o perfil.')))
  }, [])

  const salvar = async () => {
    setErro('')
    setAviso('')
    setSalvando(true)
    try {
      const p = await editarPerfil({
        nome,
        nomeDeUsuario,
        email,
        telefone: telefone || null,
        dataNascimento: dataNascimento || null,
      })
      atualizarPerfil(p.nome, p.email, p.nomeDeUsuario)
      setAviso('Perfil salvo.')
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível salvar o perfil.'))
    } finally {
      setSalvando(false)
    }
  }

  const inicial = (nome || 'U').charAt(0).toUpperCase()

  return (
    <>
      <Secao titulo="Perfil" sub="Seus dados de cadastro.">
        <div className="config-perfil">
          <span className="avatar config-avatar">{inicial}</span>
          <div>
            <div className="config-perfil-nome">{nome || 'Usuário'}</div>
            <div className="config-linha-desc">{email}</div>
          </div>
        </div>

        <div className="config-grade">
          <label className="campo">
            Nome completo
            <input type="text" maxLength={100} value={nome} onChange={(e) => setNome(e.target.value)} />
          </label>
          <label className="campo">
            Nome de usuário
            <input type="text" maxLength={50} value={nomeDeUsuario} onChange={(e) => setNomeDeUsuario(e.target.value)} />
          </label>
          <label className="campo">
            E-mail
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          </label>
          <label className="campo">
            Telefone
            <input type="tel" placeholder="(00) 00000-0000" value={telefone} onChange={(e) => setTelefone(e.target.value)} />
          </label>
          <AlterarSenha />
          <label className="campo">
            Data de nascimento
            <input type="date" value={dataNascimento} onChange={(e) => setDataNascimento(e.target.value)} />
          </label>
        </div>

        {erro && <p className="erro">{erro}</p>}
        {aviso && <p className="sucesso">{aviso}</p>}
        <div className="config-acoes">
          <button type="button" className="btn btn-primario" onClick={salvar} disabled={salvando}>
            {salvando ? 'Salvando...' : 'Salvar alterações'}
          </button>
        </div>
      </Secao>
    </>
  )
}

// Sem os itens que dependem de e-mail (duas etapas, aviso de novo acesso): saem até existirem
function AbaSeguranca() {
  const { renovarSessao } = useAuth()
  const [aviso, setAviso] = useState('')
  const [erro, setErro] = useState('')
  const [encerrando, setEncerrando] = useState(false)

  const encerrar = async () => {
    setErro('')
    setAviso('')
    setEncerrando(true)
    try {
      const { expiraEm } = await encerrarOutrasSessoes()
      renovarSessao(expiraEm)
      setAviso('Pronto: os outros dispositivos foram desconectados. Este continua conectado.')
    } catch (err: unknown) {
      setErro(extrairErro(err, 'Não foi possível encerrar as outras sessões.'))
    } finally {
      setEncerrando(false)
    }
  }

  return (
    <>
      <Secao titulo="Sessões" sub="Onde sua conta está conectada.">
        <Linha titulo="Este navegador" descricao="Sessão atual · encerra sozinha quando o acesso expira">
          <span className="tag verde">Ativa</span>
        </Linha>
        <Linha titulo="Encerrar outras sessões" descricao="Desconecta todos os outros dispositivos e navegadores.">
          <button type="button" className="btn btn-ghost" onClick={encerrar} disabled={encerrando}>
            {encerrando ? 'Encerrando...' : 'Encerrar'}
          </button>
        </Linha>
        {erro && <p className="erro">{erro}</p>}
        {aviso && <p className="sucesso">{aviso}</p>}
      </Secao>
    </>
  )
}

type ChavePreferencia =
  | 'lancamentosOutros'
  | 'vencimento'
  | 'atrasada'
  | 'saldoBaixo'
  | 'gastoAcimaNormal'
  | 'metaProgresso'
  | 'metaPrazo'

// Itens com `chave` são salvos nas preferências; os sem `chave` ficam sempre ligados
const GRUPOS_NOTIFICACAO: {
  titulo: string
  sub: string
  itens: { id: string; titulo: string; descricao: string; chave?: ChavePreferencia }[]
}[] = [
  {
    titulo: 'Contas conjuntas',
    sub: 'O que acontece nas contas que você compartilha.',
    itens: [
      { id: 'convite', titulo: 'Convites recebidos', descricao: 'Alguém convidou você para uma conta conjunta.' },
      { id: 'participantes', titulo: 'Entrada e saída de participantes', descricao: 'Alguém aceitou o convite, saiu ou foi removido da conta.' },
      { id: 'lancamento-outro', titulo: 'Lançamentos de outros participantes', descricao: 'Outra pessoa lançou, editou ou excluiu uma transação na conta conjunta.', chave: 'lancamentosOutros' },
    ],
  },
  {
    titulo: 'Contas a pagar e saldo',
    sub: 'Lembretes para não perder nenhum vencimento.',
    itens: [
      { id: 'vencimento', titulo: 'Lembrete de vencimento', descricao: 'Avisa antes de uma transação pendente vencer.', chave: 'vencimento' },
      { id: 'atrasada', titulo: 'Transação atrasada', descricao: 'Uma transação passou da data e continua pendente.', chave: 'atrasada' },
      { id: 'saldo-baixo', titulo: 'Saldo baixo', descricao: 'O saldo de uma conta ficou abaixo do valor que você definir.', chave: 'saldoBaixo' },
      { id: 'gasto-categoria', titulo: 'Gasto acima do normal', descricao: 'O gasto do mês numa categoria passou 30% da média dos 3 meses anteriores.', chave: 'gastoAcimaNormal' },
    ],
  },
  {
    titulo: 'Metas',
    sub: 'Acompanhe o progresso das suas metas.',
    itens: [
      { id: 'meta-progresso', titulo: 'Progresso da meta', descricao: 'Quando uma meta atingir 25%, 50%, 75% e 100%.', chave: 'metaProgresso' },
      { id: 'meta-prazo', titulo: 'Prazo chegando', descricao: 'Faltam 30 dias para o prazo e a meta ainda não foi atingida.', chave: 'metaPrazo' },
    ],
  },
]

function AbaNotificacoes() {
  // Notificações do navegador: escolha deste navegador, com a permissão dele
  const [navegador, setNavegador] = useState(notificacoesNavegadorLigadas)
  const [preferencias, setPreferencias] = useState<PreferenciasNotificacao | null>(null)
  const [erro, setErro] = useState('')
  // Saldo baixo: ao ligar, abre o campo em R$; só fica ligado com um valor válido
  const [campoSaldoAberto, setCampoSaldoAberto] = useState(false)
  const [valorSaldo, setValorSaldo] = useState('')

  useEffect(() => {
    let cancelado = false
    obterPreferenciasNotificacao()
      .then((p) => {
        if (cancelado) return
        setPreferencias(p)
        setValorSaldo(p.valorSaldoBaixo != null ? String(p.valorSaldoBaixo) : '')
      })
      .catch(() => {
        if (!cancelado) setErro('Não foi possível carregar suas preferências de notificação.')
      })
    return () => {
      cancelado = true
    }
  }, [])

  // Cada mudança salva na hora; se falhar, o controle volta e aparece a mensagem
  const salvar = async (mudancas: Partial<PreferenciasNotificacao>) => {
    if (!preferencias) return
    const anterior = preferencias
    setPreferencias({ ...preferencias, ...mudancas })
    setErro('')
    try {
      setPreferencias(await salvarPreferenciasNotificacao({ ...preferencias, ...mudancas }))
    } catch (err: unknown) {
      setPreferencias(anterior)
      setErro(extrairErro(err, 'Não foi possível salvar a preferência.'))
    }
  }

  const alternar = (chave: ChavePreferencia) => {
    if (!preferencias) return
    if (chave === 'saldoBaixo' && !preferencias.saldoBaixo) {
      // Liga só depois que houver um valor válido (salvo ao sair do campo)
      const valor = Number(valorSaldo)
      if (valorSaldo && valor > 0) salvar({ saldoBaixo: true, valorSaldoBaixo: valor })
      else setCampoSaldoAberto(true)
      return
    }
    if (chave === 'saldoBaixo') setCampoSaldoAberto(false)
    salvar({ [chave]: !preferencias[chave] })
  }

  const alternarNavegador = async () => {
    setErro('')
    if (navegador) {
      desligarNotificacoesNavegador()
      setNavegador(false)
      return
    }
    const resultado = await ligarNotificacoesNavegador()
    if (resultado === 'ligado') setNavegador(true)
    else if (resultado === 'negado')
      setErro('O navegador bloqueou as notificações deste site. Libere nas permissões do site e tente de novo.')
    else setErro('Este navegador não tem notificações.')
  }

  const salvarValorSaldo = () => {
    if (!preferencias) return
    const valor = Number(valorSaldo)
    if (!valorSaldo || !(valor > 0)) {
      if (!preferencias.saldoBaixo) setCampoSaldoAberto(false)
      setErro('Informe um valor maior que zero para o saldo baixo.')
      return
    }
    setCampoSaldoAberto(false)
    if (!preferencias.saldoBaixo || valor !== preferencias.valorSaldoBaixo)
      salvar({ saldoBaixo: true, valorSaldoBaixo: valor })
  }

  return (
    <>
      {erro && <p className="erro">{erro}</p>}

      <Secao titulo="Canais" sub="Por onde você quer ser avisado.">
        <Linha titulo="No app (sino)" descricao="Aparecem no sino do topo da tela.">
          <span className="tag verde">Sempre ligado</span>
        </Linha>
        <Linha
          titulo="Notificações do navegador"
          descricao="Com o app aberto, avisam quando você está em outra aba ou em outro programa. Vale para este navegador."
        >
          <Interruptor
            ligado={navegador}
            aoAlternar={alternarNavegador}
            rotulo="Notificações do navegador"
            desativado={!navegadorSuportaNotificacoes()}
          />
        </Linha>
      </Secao>

      {GRUPOS_NOTIFICACAO.map((grupo) => (
        <Secao key={grupo.titulo} titulo={grupo.titulo} sub={grupo.sub}>
          {grupo.itens.map((item) => {
            const chave = item.chave
            const ligado = chave
              ? Boolean(preferencias?.[chave]) || (chave === 'saldoBaixo' && campoSaldoAberto)
              : true
            return (
              <Linha key={item.id} titulo={item.titulo} descricao={item.descricao}>
                {chave === 'vencimento' && (
                  <select
                    className="rel-select"
                    aria-label="Dias antes do vencimento"
                    value={preferencias?.diasAntesVencimento ?? 3}
                    onChange={(e) => salvar({ diasAntesVencimento: Number(e.target.value) })}
                    disabled={!preferencias?.vencimento}
                  >
                    <option value="1">1 dia antes</option>
                    <option value="3">3 dias antes</option>
                    <option value="7">7 dias antes</option>
                  </select>
                )}
                {chave === 'saldoBaixo' && ligado && (
                  <input
                    type="number"
                    className="rel-select config-valor"
                    aria-label="Valor do saldo baixo (R$)"
                    placeholder="R$"
                    min="0.01"
                    step="0.01"
                    value={valorSaldo}
                    onChange={(e) => setValorSaldo(e.target.value)}
                    onBlur={salvarValorSaldo}
                    autoFocus={campoSaldoAberto}
                  />
                )}
                <span className="tag verde">{chave ? 'Disponível' : 'Sempre ligado'}</span>
                <Interruptor
                  ligado={ligado}
                  aoAlternar={() => chave && alternar(chave)}
                  rotulo={item.titulo}
                  desativado={!chave || !preferencias}
                />
              </Linha>
            )
          })}
        </Secao>
      ))}

      <Secao titulo="Horário de silêncio" sub="Das 22h às 7h.">
        <Linha
          titulo="Não perturbar das 22h às 7h"
          descricao="Vale para as notificações do navegador. O sino continua normal."
        >
          <span className="tag verde">Disponível</span>
          <Interruptor
            ligado={Boolean(preferencias?.horarioSilencio)}
            aoAlternar={() => preferencias && salvar({ horarioSilencio: !preferencias.horarioSilencio })}
            rotulo="Horário de silêncio"
            desativado={!preferencias}
          />
        </Linha>
      </Secao>
    </>
  )
}

const OPCOES_TEMA: { modo: ModoTema; rotulo: string }[] = [
  { modo: 'light', rotulo: 'Claro' },
  { modo: 'dark', rotulo: 'Escuro' },
  { modo: 'sistema', rotulo: 'Igual ao sistema' },
]

// Preferências reais: o tema fica neste navegador; o resto é salvo por membro no backend
// (GET/PUT api/Membro/preferencias), cada mudança na hora, e vale ao abrir as telas
function AbaPreferencias() {
  const { modo, definirModo } = useTema()
  const [preferencias, setPreferencias] = useState<PreferenciasUso>(preferenciasUso)
  const [contas, setContas] = useState<OpcaoConta[]>([])
  const [erro, setErro] = useState('')

  useEffect(() => {
    let cancelado = false
    Promise.all([obterPreferenciasUso(), carregarOpcoesConta()])
      .then(([p, opcoes]) => {
        if (cancelado) return
        guardarPreferenciasUso(p)
        setPreferencias(p)
        setContas(opcoes.filter((o) => o.liberada))
      })
      .catch(() => {
        if (!cancelado) setErro('Não foi possível carregar suas preferências.')
      })
    return () => {
      cancelado = true
    }
  }, [])

  // Cada mudança salva na hora; se falhar, o controle volta e aparece a mensagem
  const salvar = async (mudancas: Partial<PreferenciasUso>) => {
    const anterior = preferencias
    const novas = { ...preferencias, ...mudancas }
    setPreferencias(novas)
    setErro('')
    try {
      const salvas = await salvarPreferenciasUso(novas)
      guardarPreferenciasUso(salvas)
      setPreferencias(salvas)
    } catch (err: unknown) {
      setPreferencias(anterior)
      setErro(extrairErro(err, 'Não foi possível salvar a preferência.'))
    }
  }

  // Conta padrão que deixou de existir (ou ficou sem saldo inicial) cai na principal
  const contaPadraoValida = contas.some((o) => o.chave === preferencias.contaPadrao)
    ? (preferencias.contaPadrao ?? '')
    : ''

  return (
    <>
      {erro && <p className="erro">{erro}</p>}

      <Secao titulo="Aparência">
        <Linha titulo="Tema" descricao="Vale para este navegador. O botão de sol/lua no topo também troca.">
          <div className="btn-grupo" role="group" aria-label="Tema">
            {OPCOES_TEMA.map((o) => (
              <button
                key={o.modo}
                type="button"
                className={modo === o.modo ? 'ativo' : ''}
                aria-pressed={modo === o.modo}
                onClick={() => definirModo(o.modo)}
              >
                {o.rotulo}
              </button>
            ))}
          </div>
        </Linha>
      </Secao>

      <Secao titulo="Regional" sub="Formatos usados nas telas, PDFs e planilhas.">
        <Linha titulo="Moeda e formato de data">
          <span className="config-linha-desc">Real (R$) · dd/mm/aaaa</span>
        </Linha>
        <Linha titulo="Primeiro dia da semana" descricao='Usado no filtro "Esta semana".'>
          <select
            className="rel-select"
            aria-label="Primeiro dia da semana"
            value={preferencias.semanaComecaNoDomingo ? 'domingo' : 'segunda'}
            onChange={(e) => salvar({ semanaComecaNoDomingo: e.target.value === 'domingo' })}
          >
            <option value="segunda">Segunda-feira</option>
            <option value="domingo">Domingo</option>
          </select>
        </Linha>
      </Secao>

      <Secao titulo="Uso do app">
        <Linha titulo="Conta padrão para novas transações" descricao="Já vem selecionada ao lançar uma transação.">
          <select
            className="rel-select"
            aria-label="Conta padrão para novas transações"
            value={contaPadraoValida}
            onChange={(e) => salvar({ contaPadrao: e.target.value || null })}
          >
            <option value="">Conta principal</option>
            {contas
              .filter((o) => !o.principal)
              .map((o) => (
                <option key={o.chave} value={o.chave}>
                  {o.nome}
                  {o.conjunta ? ' (conjunta)' : ''}
                </option>
              ))}
          </select>
        </Linha>
        <Linha titulo="Período padrão dos Relatórios">
          <select
            className="rel-select"
            aria-label="Período padrão dos Relatórios"
            value={preferencias.periodoRelatorios}
            onChange={(e) => salvar({ periodoRelatorios: Number(e.target.value) })}
          >
            <option value="1">Mês</option>
            <option value="3">Trimestre</option>
            <option value="6">Semestre</option>
            <option value="12">Ano</option>
          </select>
        </Linha>
        <Linha titulo="Ocultar valores ao abrir" descricao="Os cards do painel mostram •••• até você tocar para ver.">
          <Interruptor
            ligado={preferencias.ocultarValores}
            aoAlternar={() => salvar({ ocultarValores: !preferencias.ocultarValores })}
            rotulo="Ocultar valores ao abrir"
          />
        </Linha>
      </Secao>
    </>
  )
}

function AbaPrivacidade() {
  const { membroId, logout } = useAuth()
  const navigate = useNavigate()
  const [exportando, setExportando] = useState(false)
  const [erroExportar, setErroExportar] = useState('')
  // Excluir conta: abre o campo de senha antes de confirmar
  const [confirmando, setConfirmando] = useState(false)
  const [senha, setSenha] = useState('')
  const [erroExcluir, setErroExcluir] = useState('')
  const [excluindo, setExcluindo] = useState(false)

  const exportar = async () => {
    if (membroId == null) return
    setErroExportar('')
    setExportando(true)
    try {
      baixarArquivo(await gerarExcelMeusDados(membroId), nomeArquivoMeusDados())
    } catch (err: unknown) {
      setErroExportar(extrairErro(err, 'Não foi possível exportar os seus dados.'))
    } finally {
      setExportando(false)
    }
  }

  const excluir = async () => {
    setErroExcluir('')
    setExcluindo(true)
    try {
      await excluirConta(senha)
      // A API já apagou o cookie: limpa a sessão desta tela e volta ao login
      logout()
      navigate('/login', { replace: true })
    } catch (err: unknown) {
      setErroExcluir(extrairErro(err, 'Não foi possível excluir a conta.'))
      setExcluindo(false)
    }
  }

  return (
    <>
      <Secao titulo="Seus dados" sub="Leve uma cópia de tudo o que está no app.">
        <Linha titulo="Exportar meus dados" descricao="Contas, transações, categorias e metas em uma planilha do Excel.">
          <button type="button" className="btn btn-ghost" onClick={exportar} disabled={exportando}>
            {exportando ? 'Exportando...' : 'Exportar'}
          </button>
        </Linha>
        {erroExportar && <p className="erro">{erroExportar}</p>}
      </Secao>

      <Secao titulo="Zona de perigo">
        <Linha
          titulo="Excluir minha conta"
          descricao="Desativa sua conta, tira você das contas conjuntas, para as transações recorrentes e encerra todas as sessões. Essa ação não pode ser desfeita."
        >
          {!confirmando && (
            <button type="button" className="btn btn-perigo" onClick={() => setConfirmando(true)}>
              Excluir conta
            </button>
          )}
        </Linha>
        {confirmando && (
          <div className="config-grade-inteira">
            <label className="campo">
              Digite a sua senha para confirmar
              <input
                type="password"
                autoComplete="current-password"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                autoFocus
              />
            </label>
            {erroExcluir && <p className="erro">{erroExcluir}</p>}
            <div className="config-acoes">
              <button
                type="button"
                className="btn btn-ghost"
                onClick={() => {
                  setConfirmando(false)
                  setSenha('')
                  setErroExcluir('')
                }}
                disabled={excluindo}
              >
                Cancelar
              </button>
              <button type="button" className="btn btn-perigo" onClick={excluir} disabled={excluindo || !senha}>
                {excluindo ? 'Excluindo...' : 'Excluir definitivamente'}
              </button>
            </div>
          </div>
        )}
      </Secao>
    </>
  )
}

/* ---------- Página ---------- */

export default function Configuracoes() {
  const [params, setParams] = useSearchParams()
  const abaUrl = params.get('aba') as Aba | null
  const aba: Aba = ABAS.some((a) => a.id === abaUrl) ? (abaUrl as Aba) : 'conta'

  return (
    <div className="pagina">
      <div className="pagina-cabecalho">
        <div>
          <h1 className="pagina-titulo">Configurações</h1>
          <p className="pagina-subtitulo">Gerencie sua conta, a segurança e como o app avisa você.</p>
        </div>
      </div>

      <div className="config-layout">
        <nav className="config-abas" aria-label="Seções das configurações">
          {ABAS.map((a) => (
            <button
              key={a.id}
              type="button"
              className={`config-aba ${aba === a.id ? 'ativa' : ''}`}
              aria-current={aba === a.id ? 'page' : undefined}
              onClick={() => setParams(a.id === 'conta' ? {} : { aba: a.id })}
            >
              {a.icone}
              <span>{a.rotulo}</span>
            </button>
          ))}
        </nav>

        <div className="config-conteudo">
          {aba === 'conta' && <AbaConta />}
          {aba === 'seguranca' && <AbaSeguranca />}
          {aba === 'notificacoes' && <AbaNotificacoes />}
          {aba === 'preferencias' && <AbaPreferencias />}
          {aba === 'privacidade' && <AbaPrivacidade />}
        </div>
      </div>
    </div>
  )
}
