using GerenciadorContaFamiliar.Domain;

namespace GerenciadorContaFamiliar.Domain.Shared.Interfaces;
    /// <summary>
    /// Interface do repositório para entidade Transacao.
    /// Define operações de acesso a dados específicas do domínio de transações financeiras.
    /// Implementada na camada de Infraestrutura (ex: EntityFramework).
    /// </summary>
    public interface ITransacaoRepository
    {
        /// <summary>
        /// Obtém uma transação pelo seu identificador único.
        /// </summary>
        /// <param name="id">Identificador único da transação</param>
        /// <param name="ct">Token de cancelamento (opcional)</param>
        /// <returns>Transação encontrada ou null se não existir</returns>
        Task<Transacao?> ObterPorIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Obtém todas as transações associadas a uma conta específica.
        /// Inclui opcionalmente transações de contas conjuntas.
        /// </summary>
        /// <param name="contaId">Identificador da conta principal</param>
        /// <param name="apenasDaContaPrincipal">Se true, retorna apenas transações da conta principal (exclui contas conjuntas)</param>
        /// <param name="ct">Token de cancelamento (opcional)</param>
        /// <returns>Lista somente leitura de transações</returns>
        Task<IReadOnlyList<Transacao>> ObterPorContaAsync(
            int contaId, 
            CancellationToken ct = default);

        /// <summary>
        /// Obtém as transações associadas a uma conta conjunta específica.
        /// </summary>
        /// <param name="contaConjuntaId">Identificador da conta conjunta</param>
        /// <param name="ct">Token de cancelamento (opcional)</param>
        /// <returns>Lista somente leitura de transações</returns>
        Task<IReadOnlyList<Transacao>> ObterPorContaConjuntaAsync(
            int contaConjuntaId,
            CancellationToken ct = default);

        /// <summary>
        /// Obtém transações dentro de um intervalo de datas específico.
        /// Útil para relatórios mensais, anuais ou personalizados.
        /// </summary>
        /// <param name="dataInicio">Data inicial do período (inclusive)</param>
        /// <param name="dataFim">Data final do período (inclusive)</param>
        /// <param name="ct">Token de cancelamento (opcional)</param>
        /// <returns>Lista somente leitura de transações no período especificado</returns>
        Task<IReadOnlyList<Transacao>> ObterPorPeriodoAsync(
            DateTime dataInicio, 
            DateTime dataFim, 
            CancellationToken ct = default);

        /// <summary>
        /// Obtém transações filtradas para o relatório de gastos.
        /// Já inclui as navegações Categoria e SubCategoria carregadas.
        /// Considera apenas as contas do membro (individuais dele e conjuntas das quais participa).
        /// </summary>
        /// <param name="membroId">Membro dono das contas consultadas (obrigatório)</param>
        /// <param name="idCategoria">Filtra por categoria (opcional)</param>
        /// <param name="idSubCategoria">Filtra por subcategoria (opcional)</param>
        /// <param name="apenasSemSubcategoria">Se true, retorna apenas transações SEM subcategoria</param>
        /// <param name="dataInicio">Período inicial (opcional)</param>
        /// <param name="dataFim">Período final (opcional)</param>
        /// <param name="ct">Token de cancelamento (opcional)</param>
        Task<IReadOnlyList<Transacao>> ObterParaRelatorioAsync(
            int membroId,
            int? idCategoria,
            int? idSubCategoria,
            bool apenasSemSubcategoria = false,
            DateOnly? dataInicio = null,
            DateOnly? dataFim = null,
            CancellationToken ct = default);

        /// <summary>
        /// Adiciona uma nova transação ao repositório.
        /// A transação será persistida no banco de dados quando o UnitOfWork for commitado.
        /// </summary>
        /// <param name="transacao">Transação a ser adicionada</param>
        /// <param name="ct">Token de cancelamento (opcional)</param>
        /// <summary>
        /// Soma das saídas ativas (qualquer status) de uma categoria no período, incluindo as
        /// transações adicionadas e ainda não gravadas nesta requisição.
        /// </summary>
        Task<decimal> SomarSaidasDaCategoriaAsync(
            int categoriaId,
            DateOnly dataInicio,
            DateOnly dataFim,
            CancellationToken ct = default);

        /// <summary>
        /// Efeito no saldo do que ainda está em aberto numa conta (individual ou conjunta):
        /// entradas − saídas ativas PENDENTES ou ATRASADAS. Somado ao saldo, dá o saldo previsto.
        /// </summary>
        Task<decimal> SomarEmAbertoAsync(int? contaId, int? contaConjuntaId, CancellationToken ct = default);

        /// <summary>
        /// Transações ativas PENDENTES ou ATRASADAS das contas que o membro acessa (individuais
        /// dele e conjuntas de que participa). Rastreadas: a verificação muda o status.
        /// </summary>
        Task<IReadOnlyList<Transacao>> ObterPendentesEAtrasadasDoMembroAsync(
            int membroId,
            CancellationToken ct = default);

        Task AdicionarAsync(Transacao transacao, CancellationToken ct = default);

        /// <summary>
        /// Ocorrências ativas e não concluídas (PENDENTES ou ATRASADAS) de uma série com data
        /// posterior à informada (com a conta carregada, para o estorno do saldo).
        /// </summary>
        Task<IReadOnlyList<Transacao>> ObterEmAbertoDaSerieAposAsync(
            int recorrenciaId,
            DateOnly data,
            CancellationToken ct = default);

        /// <summary>
        /// Atualiza uma transação existente no repositório.
        /// As alterações serão persistidas no banco de dados quando o UnitOfWork for commitado.
        /// </summary>
        /// <param name="transacao">Transação com os dados atualizados</param>
        /// <param name="ct">Token de cancelamento (opcional)</param>
        Task AtualizarAsync(Transacao transacao, CancellationToken ct = default);

        /// <summary>
        /// Remove uma transação pelo seu identificador.
        /// A remoção será persistida no banco de dados quando o UnitOfWork for commitado.
        /// </summary>
        /// <param name="id">Identificador único da transação a ser removida</param>
        /// <param name="ct">Token de cancelamento (opcional)</param>
        Task RemoverAsync(int id, CancellationToken ct = default);
    }