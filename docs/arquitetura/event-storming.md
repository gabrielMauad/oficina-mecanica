# Event Storming — Contextos Delimitados
## Sistema de Oficina Mecânica

---

## Legenda

| Sigla | Tipo | Descrição |
|-------|------|-----------|
| ML | Modelo de Leitura | Tela ou dado que o ator consulta antes de agir |
| AT | Ator | Quem executa o comando |
| CMD | Comando | Ação intencional que muda o estado do sistema |
| AG | Agregado | Entidade de domínio responsável por guardar o estado |
| EV | Evento | Fato imutável que ocorreu como resultado de um comando |
| POL | Política | Regra de negócio que reage a um evento e dispara um novo comando |
| SE | Sistema Externo | Fim do fluxo no contexto, delegado a um sistema externo |

**Padrão de fluxo:**
```
[ML] → [AT] → [CMD] → [AG] → [EV] → [POL] → [CMD] → ...
                                    ↘ [AT] → [CMD] → ...
```

---

## Contexto Delimitado: CADASTRO

**Agregado:** Cliente · Veículo · Serviço

### Fluxo 1 — Cadastrar Cliente

```
[ML] Tela de formulário de cadastro
  → [AT] Funcionário
    → [CMD] Cadastrar Cliente
      → [AG] Cliente
        → [EV] Cliente Cadastrado
```

### Fluxo 2 — Cadastrar Veículo

```
[ML] Tela de formulário de cadastro
  → [AT] Funcionário
    → [CMD] Cadastrar Veículo
      → [AG] Veículo
        → [EV] Veículo Cadastrado
```

### Fluxo 3 — Adicionar Serviço ao Catálogo

```
[ML] Tela de formulário de cadastro
  → [AT] Funcionário
    → [CMD] Adicionar Serviços
      → [AG] Serviço
        → [EV] Serviço Adicionado
```

> Os três fluxos de cadastro são independentes entre si e podem ocorrer em qualquer ordem.
> O cadastro de Cliente e Veículo é pré-requisito para a abertura de uma OS.

---

## Contexto Delimitado: ORDEM DE SERVIÇO

**Agregado:** Ordem de Serviço

### Fluxo Principal — Caminho Feliz

#### Etapa 1 — Criação da OS

```
[ML] Tela de sistema de OS
  → [AT] Funcionário
    → [CMD] Gerar OS
      → [AG] Ordem de Serviço
        → [EV] OS Criada
          → [POL] Alterar status para OS recebida
```

#### Etapa 2 — Recepção da OS

```
[POL] Alterar status para OS recebida
  → [CMD] Receber OS
    → [AG] Ordem de Serviço
      → [EV] OS Recebida
```

> **Correção aplicada:** O diagrama original nomeia o CMD como "Alterar status para OS recebida" (mesma descrição da POL).
> CMD deve ser verbo imperativo que representa uma intenção ("Receber OS"), não o resultado da ação.

#### Etapa 3 — Início do Diagnóstico

```
[AT] Funcionário
  → [CMD] Iniciar Diagnóstico
    → [AG] Ordem de Serviço
      → [EV] Diagnóstico Iniciado
        → [POL] Alterar status para OS em diagnóstico
```

#### Etapa 4 — Realização do Diagnóstico

```
[POL] Alterar status para OS em diagnóstico
  → [CMD] Registrar Diagnóstico
    → [AG] Ordem de Serviço
      → [EV] Análise Realizada e Peças Identificadas
```

> **Correção aplicada:** O diagrama original nomeia o CMD como "Alterar o status da OS para diagnóstico",
> que é novamente a descrição do resultado, não da ação. O CMD deve expressar a intenção do ator
> ("Registrar Diagnóstico"). O EV resultante ("Análise Realizada e Peças Identificadas") representa
> o fato de negócio que importa para os demais contextos.

> **Ponto de integração:** Este EV dispara o fluxo no Contexto de Peças e Insumos (ver seção de Fluxos Cross-Contexto).

#### Etapa 5 — Geração do Orçamento

```
[CMD] Gerar Orçamento
  → [AG] Ordem de Serviço
    → [EV] Orçamento Gerado
      → [POL] Enviar Orçamento para o Cliente
```

> É disparado de forma:
> - Automaticamente pela POL "Gerar Orçamento Automaticamente" vinda do Contexto de Peças e Insumos

#### Etapa 6 — Envio do Orçamento

```
[POL] Enviar Orçamento para o Cliente
  → [CMD] Enviar Orçamento
    → [AG] Ordem de Serviço
      → [EV] Orçamento Enviado para o Cliente
```

#### Etapa 7 — Aprovação do Orçamento pelo Cliente

```
[ML] E-mail recebido pelo cliente
  → [AT] Cliente
    → [CMD] Aprovar Orçamento
      → [AG] Ordem de Serviço
        → [EV] Orçamento Aprovado
```

#### Etapa 8 — Execução da OS

```
[AT] Mecânico
  → [CMD] Executar OS
    → [AG] Ordem de Serviço
      → [EV] OS em Execução
```

#### Etapa 9 — Finalização da OS

```
[ML] Tela de sistema de OS
  → [AT] Funcionário
    → [CMD] Finalizar OS
      → [AG] Ordem de Serviço
        → [EV] OS Finalizada
          → [POL] Avisar Cliente que o Serviço foi Finalizado
```

#### Etapa 10 — Notificação e Retirada do Veículo

```
[POL] Avisar Cliente que o Serviço foi Finalizado
  → [CMD] Notificar Cliente
    → [EV] Cliente Notificado
      → [AT] Cliente
        → [CMD] Retirar Veículo
          → [EV] Cliente Retirou o Veículo
```

> **Correção aplicada:** O diagrama original mapeia "Cliente notificado" como CMD e
> "Cliente retirou o veículo" como EV na mesma linha, sem um AT intermediando a retirada.
> Pela lógica do negócio, a notificação gera o EV "Cliente Notificado", e a retirada
> é uma ação do AT "Cliente" disparando um novo CMD.

#### Etapa 11 — Conclusão e Entrega da OS

```
[ML] Tela de sistema de OS
  → [AT] Funcionário
    → [CMD] Concluir OS
      → [AG] Ordem de Serviço
        → [EV] OS Entregue
```

---

## Contexto Delimitado: PEÇAS E INSUMOS

**Agregado:** Estoque · Peça

### Fluxo 1 — Consulta ao Estoque

```
[ML] Tela de busca de peças no estoque ou solicitação para serviço externo
  → [AT] Funcionário
    → [CMD] Buscar Peças no Estoque
      → [AG] Estoque
        → [EV] Peças Consultadas
          → [POL] Validar Quantidade das Peças
```

### Fluxo 2a — Estoque Disponível (caminho feliz)

```
[POL] Validar Quantidade das Peças
  → [CMD] Validar Quantidade
    → [AG] Estoque
      → [EV] Estoque Disponível
        → [POL] Atualizar Quantidade de Peças no Estoque
```

```
[POL] Atualizar Quantidade de Peças no Estoque
  → [CMD] Atualizar Estoque
    → [AG] Estoque
      → [EV] Estoque Atualizado
        → [POL] Atualiza a OS com a Peça do Estoque
```

```
[POL] Atualiza a OS com a Peça do Estoque
  → [CMD] Vincular Peça
    → [AG] Estoque / Ordem de Serviço
      → [EV] Peças Adicionadas
        → [POL] Gerar Orçamento Automaticamente
          → [CMD] Gerar Orçamento  ← retorna ao Contexto OS (Etapa 5)
```

### Fluxo 2b — Estoque Esgotado (caminho alternativo)

```
[POL] Validar Quantidade das Peças
  → [CMD] Validar Quantidade
    → [AG] Estoque
      → [EV] Estoque Esgotado
        → [POL] Impedir a Inclusão da Peça na OS
```

> Quando o estoque está esgotado, a peça não pode ser vinculada à OS.
> O fluxo alternativo é solicitar a peça externamente (Fluxo 3).

### Fluxo 3 — Solicitação de Peças Externas (caminho alternativo)

```
[CMD] Solicitar Peças
  → [SE] Serviço Externo
```

> Fim do fluxo no contexto. A resposta do serviço externo (entrega das peças)
> reiniciaria o fluxo de vinculação (Fluxo 2a) quando as peças chegarem.

---

## Fluxos Cross-Contexto

### OS → Peças e Insumos

Disparado quando o mecânico identifica as peças necessárias durante o diagnóstico.

```
[EV] Análise Realizada e Peças Identificadas  (Contexto: OS)
  ↓
[AT] Funcionário → [CMD] Buscar Peças no Estoque  (Contexto: Peças e Insumos)
```

### Peças e Insumos → OS (fluxo circular)

Disparado quando as peças são vinculadas com sucesso à OS.

```
[EV] Peças Adicionadas  (Contexto: Peças e Insumos)
  → [POL] Gerar Orçamento Automaticamente
    ↓
[CMD] Gerar Orçamento  (Contexto: OS — Etapa 5)
```

---

## Visão Geral do Fluxo Completo

```
CADASTRO                   ORDEM DE SERVIÇO                     PEÇAS E INSUMOS
─────────────────────────────────────────────────────────────────────────────────
Cadastrar Cliente
Cadastrar Veículo
Adicionar Serviços
        │
        └──────────────► Gerar OS
                              │
                         OS Criada
                              │
                         OS Recebida
                              │
                         Iniciar Diagnóstico
                              │
                         Análise Realizada ──────────────────► Buscar Peças
                         e Peças Identificadas                       │
                              │                              Peças Consultadas
                              │                                      │
                              │                              Validar Quantidade
                              │                               ┌──────┴──────┐
                              │                          Disponível     Esgotado
                              │                               │              │
                              │                       Atualizar Estoque  Impedir
                              │                               │         Inclusão
                              │                       Vincular Peça
                              │                               │
                              │◄──────────────────── Peças Adicionadas
                              │              (POL: Gerar Orçamento Automaticamente)
                         Gerar Orçamento
                              │
                         Orçamento Gerado
                              │
                         Enviar Orçamento
                              │
                         Orçamento Enviado
                              │
                     Cliente: Aprovar Orçamento
                              │
                         Orçamento Aprovado
                              │
                     Mecânico: Executar OS
                              │
                         OS em Execução
                              │
                         Finalizar OS
                              │
                         OS Finalizada
                              │
                         Notificar Cliente
                              │
                         Cliente Notificado
                              │
                     Cliente: Retirar Veículo
                              │
                         Cliente Retirou o Veículo
                              │
                         Concluir OS
                              │
                         OS Entregue ✓
```

---

## Correções Aplicadas

| # | Original no diagrama | Correção aplicada | Motivo |
|---|---|---|---|
| 1 | CMD "Alterar status para OS recebida" | CMD "Receber OS" | CMD deve ser verbo imperativo (intenção), não descrever o resultado |
| 2 | CMD "Alterar o status da OS para diagnóstico" | CMD "Registrar Diagnóstico" | Mesmo motivo — a ação real é registrar, não alterar status |
| 3 | CMD "Cliente notificado" | CMD "Notificar Cliente" + EV "Cliente Notificado" | "Notificado" é estado passado (EV), não ação (CMD) |
| 4 | EV → EV sem CMD intermediário | AT/POL → CMD → EV | Padrão ES: um EV nunca dispara outro EV diretamente |
| 5 | Ausência de AT "Cliente" antes da retirada | AT "Cliente" → CMD "Retirar Veículo" → EV "Cliente Retirou o Veículo" | A retirada é uma ação intencional do cliente, exige AT + CMD |
