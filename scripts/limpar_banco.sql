-- ============================================================
-- SCRIPT DE LIMPEZA DO BANCO — GerenciadorContaFamiliar
-- ============================================================
-- ATENÇÃO: apaga TODOS os dados (membros, contas, transações,
-- categorias, metas, notificações, logs...).
-- NÃO É POSSÍVEL DESFAZER. Execute por sua conta e risco.
--
-- Por que os DELETEs são nessa ordem?
-- As FKs são "Restrict" (não permitem apagar um registro que
-- ainda é referenciado por outro). Então apagamos primeiro as
-- tabelas "filhas" (que têm FK) e depois as "pais".
-- ============================================================

BEGIN;

-- ------------------------------------------------------------
-- 1) Tabelas de junção (N:N) — apagar primeiro (não têm dependentes)
-- ------------------------------------------------------------
DELETE FROM "CategoriaSubCategoria";   -- junção Categoria <-> SubCategoria
DELETE FROM "ContaConjuntaMembro";     -- junção ContaConjunta <-> Membro

-- ------------------------------------------------------------
-- 2) Entidades dependentes (têm FK para várias tabelas)
-- ------------------------------------------------------------
DELETE FROM "Transacoes";              -- FK -> Conta, ContaConjunta, Membro, Categoria
DELETE FROM "Notificacoes";            -- FK -> Membro
DELETE FROM "Metas";                   -- FK -> Membro
DELETE FROM "LogsAuditoria";           -- FK -> Membro

-- ------------------------------------------------------------
-- 3) Categorias e SubCategorias (FK -> Conta / ContaConjunta)
-- ------------------------------------------------------------
DELETE FROM "SubCategorias";
DELETE FROM "Categorias";

-- ------------------------------------------------------------
-- 4) Entidades "pais" (ninguém mais as referencia)
-- ------------------------------------------------------------
DELETE FROM "ContasConjuntas";
DELETE FROM "Contas";
DELETE FROM "Membros";

COMMIT;

-- ============================================================
-- OPCIONAL: resetar os contadores de ID (para voltar a 1)
-- Descomente se quiser que os próximos registros comecem no Id 1
-- ============================================================
-- ALTER SEQUENCE "Membros_Id_seq" RESTART WITH 1;
-- ALTER SEQUENCE "Contas_Id_seq" RESTART WITH 1;
-- ALTER SEQUENCE "ContasConjuntas_Id_seq" RESTART WITH 1;
-- ALTER SEQUENCE "Categorias_Id_seq" RESTART WITH 1;
-- ALTER SEQUENCE "SubCategorias_Id_seq" RESTART WITH 1;
-- ALTER SEQUENCE "Transacoes_Id_seq" RESTART WITH 1;
-- ALTER SEQUENCE "Metas_Id_seq" RESTART WITH 1;
-- ALTER SEQUENCE "Notificacoes_Id_seq" RESTART WITH 1;
-- ALTER SEQUENCE "LogsAuditoria_Id_seq" RESTART WITH 1;
