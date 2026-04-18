# Database Schema — Oficina Mecânica

> Schema revisado e validado contra o event storming e os requisitos do Tech Challenge.
> Fonte de verdade para as migrations EF Core de cada módulo.

---

## Regra de FK entre schemas

| Situação | FK? | Motivo |
|---|---|---|
| Tabelas no **mesmo schema** (mesmo BC) | ✅ Sim | Mesma unidade transacional, banco pode validar integridade |
| Tabelas em **schemas diferentes** (BCs diferentes) | ❌ Não | Simula isolamento de microsserviços; validação fica na Application via ACL |

Exemplos concretos:
- `cadastro.veiculo.cliente_id` → `cadastro.cliente.id` — **tem FK** (mesmo BC)
- `ordem_servico.ordem_servico.cliente_id` → `cadastro.cliente.id` — **sem FK** (BCs distintos)

---

## Schema `cadastro`

```sql
CREATE SCHEMA cadastro;

CREATE TABLE cadastro.cliente (
    id          UUID PRIMARY KEY,
    nome        VARCHAR(200)  NOT NULL,
    documento   VARCHAR(14)   NOT NULL UNIQUE, -- CPF (11 dígitos) ou CNPJ (14 dígitos), só dígitos
    email       VARCHAR(200),
    telefone    VARCHAR(20),
    ativo       BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ   NOT NULL,
    updated_at  TIMESTAMPTZ   NOT NULL
);

CREATE TABLE cadastro.veiculo (
    id          UUID PRIMARY KEY,
    placa       VARCHAR(8)    NOT NULL UNIQUE, -- Mercosul (ABC1D23) ou padrão antigo (ABC-1234)
    modelo      VARCHAR(100)  NOT NULL,
    marca       VARCHAR(100)  NOT NULL,
    ano         SMALLINT      NOT NULL,
    cliente_id  UUID          NOT NULL REFERENCES cadastro.cliente(id), -- FK intra-schema
    created_at  TIMESTAMPTZ   NOT NULL,
    updated_at  TIMESTAMPTZ   NOT NULL
);

CREATE TABLE cadastro.servico (
    id          UUID PRIMARY KEY,
    nome        VARCHAR(200)  NOT NULL,
    descricao   TEXT,
    preco_base  NUMERIC(10,2) NOT NULL,
    ativo       BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ   NOT NULL,
    updated_at  TIMESTAMPTZ   NOT NULL
);
```

---

## Schema `ordem_servico`

```sql
CREATE SCHEMA ordem_servico;

CREATE TABLE ordem_servico.ordem_servico (
    id                      UUID PRIMARY KEY,
    cliente_id              UUID          NOT NULL, -- sem FK: referência cross-schema para cadastro.cliente
    veiculo_id              UUID          NOT NULL, -- sem FK: referência cross-schema para cadastro.veiculo
    status                  VARCHAR(30)   NOT NULL, -- ver enum abaixo
    descricao_diagnostico   TEXT,                  -- preenchido no CMD RegistrarDiagnostico
    notificado_em           TIMESTAMPTZ,            -- preenchido no CMD NotificarCliente
    entregue_em             TIMESTAMPTZ,            -- preenchido no CMD ConcluirOS
    created_at              TIMESTAMPTZ   NOT NULL,
    updated_at              TIMESTAMPTZ   NOT NULL
);

-- Enum de status da OS (mantido como VARCHAR no banco; tipado como enum no domínio C#)
-- Recebida → EmDiagnostico → OrcamentoPendente → OrcamentoEnviado
-- → OrcamentoAprovado → EmExecucao → Finalizada → Entregue

CREATE TABLE ordem_servico.os_servico (
    id                       UUID PRIMARY KEY,
    ordem_servico_id         UUID          NOT NULL REFERENCES ordem_servico.ordem_servico(id),
    servico_id               UUID          NOT NULL, -- sem FK: referência cross-schema para cadastro.servico
    quantidade               INTEGER       NOT NULL,
    preco_unitario_snapshot  NUMERIC(10,2) NOT NULL  -- snapshot do preco_base no momento do diagnóstico
);

CREATE TABLE ordem_servico.os_peca (
    id                       UUID PRIMARY KEY,
    ordem_servico_id         UUID          NOT NULL REFERENCES ordem_servico.ordem_servico(id),
    peca_insumo_id           UUID          NOT NULL, -- sem FK: referência cross-schema para pecas_insumos.peca_insumo
    quantidade               INTEGER       NOT NULL,
    preco_unitario_snapshot  NUMERIC(10,2) NOT NULL  -- snapshot do preco_unitario no momento da vinculação
);

CREATE TABLE ordem_servico.orcamento (
    id                UUID PRIMARY KEY,
    ordem_servico_id  UUID          NOT NULL REFERENCES ordem_servico.ordem_servico(id),
    valor_total       NUMERIC(10,2) NOT NULL,
    status            VARCHAR(20)   NOT NULL, -- Pendente | Enviado | Aprovado | Rejeitado
    data_geracao      TIMESTAMPTZ   NOT NULL,
    data_envio        TIMESTAMPTZ,
    data_aprovacao    TIMESTAMPTZ
);
```

### Por que não há `orcamento_item`

O orçamento é calculado a partir de `os_servico` e `os_peca`, que já carregam o snapshot de preço do momento em que os itens foram adicionados. O `orcamento` armazena apenas o total e o ciclo de aprovação. Criar uma terceira cópia dos itens seria duplicação sem valor para o MVP.

Se no futuro o orçamento precisar de itens com desconto manual ou ajuste de preço, `orcamento_item` pode ser introduzido sem quebrar o schema existente.

---

## Schema `pecas_insumos`

```sql
CREATE SCHEMA pecas_insumos;

CREATE TABLE pecas_insumos.peca_insumo (
    id                  UUID PRIMARY KEY,
    nome                VARCHAR(200)  NOT NULL,
    descricao           TEXT,
    preco_unitario      NUMERIC(10,2) NOT NULL,
    quantidade_estoque  INTEGER       NOT NULL DEFAULT 0,
    unidade_medida      VARCHAR(20)   NOT NULL, -- ex.: "un", "litro", "metro"
    ativo               BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ   NOT NULL,
    updated_at          TIMESTAMPTZ   NOT NULL
);
```

---

## Rastreabilidade: event storming → coluna

| Evento do event storming | Onde fica no banco |
|---|---|
| OS Criada | `ordem_servico` INSERT com `status = Recebida` |
| Diagnóstico Iniciado | `status = EmDiagnostico` |
| Análise Realizada e Peças Identificadas | `descricao_diagnostico` preenchido + registros em `os_servico`/`os_peca` |
| Estoque Atualizado | `peca_insumo.quantidade_estoque` decrementado |
| Orçamento Gerado | INSERT em `orcamento` com `status = Pendente`, `valor_total` calculado |
| Orçamento Enviado | `orcamento.status = Enviado`, `data_envio` preenchido |
| Orçamento Aprovado | `orcamento.status = Aprovado`, `data_aprovacao` preenchido |
| OS em Execução | `ordem_servico.status = EmExecucao` |
| OS Finalizada | `ordem_servico.status = Finalizada` |
| Cliente Notificado | `ordem_servico.notificado_em` preenchido |
| OS Entregue | `ordem_servico.status = Entregue`, `entregue_em` preenchido |

---

## Resumo de tabelas por schema

| Schema | Tabelas | Observação |
|---|---|---|
| `cadastro` | `cliente`, `veiculo`, `servico` | FK intra-schema: `veiculo.cliente_id → cliente.id` |
| `ordem_servico` | `ordem_servico`, `os_servico`, `os_peca`, `orcamento` | FKs intra-schema entre as quatro tabelas; referências cross-schema só por uuid |
| `pecas_insumos` | `peca_insumo` | Sem dependências internas |
