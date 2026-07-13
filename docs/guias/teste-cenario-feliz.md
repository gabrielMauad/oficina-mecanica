# Guia de Testes E2E — Cenário Feliz (Happy Path)

> **Pré-requisito:** `docker compose up --build` rodando.
> Base URL: `http://localhost:8080` (porta configurada no docker-compose).
> Scalar/Swagger disponível em `http://localhost:8080/scalar`.
>
> **Como usar este guia:**
> Cada passo retorna um ID nos campos indicados. Copie-o e substitua nos passos
> subsequentes onde indicado com `{VARIAVEL}`.

---

## Visão geral do fluxo

```
[1] Criar Cliente
[2] Cadastrar Veículo (vinculado ao cliente)
[3] Adicionar Serviço ao catálogo
[4] Adicionar Peça/Insumo ao estoque
[5] Incrementar estoque da peça
[6] Criar Ordem de Serviço (OS)
[7] Iniciar Diagnóstico
[8] Registrar Diagnóstico  ← dispara automaticamente:
                               - Geração e envio do orçamento (OS → AguardandoAprovacao)
                               - Decremento de estoque (integration event)
[9] Verificar estoque decrementado
[10] Aprovar Orçamento
[11] Executar OS
[12] Finalizar OS           ← dispara automaticamente:
                               - Notificação ao cliente (notificado_em preenchido)
[13] Concluir OS
[14] Verificar estado final
[15] Consultar OS por Cliente
[16] Consultar Status Resumido da OS
[17] Listar OS para Acompanhamento (painel interno)
```

> ℹ️ Existe também um fluxo alternativo que colapsa os passos [6]–[8] em uma única
> chamada: `POST /ordens-servico/completa`. Veja a seção "Fluxo Alternativo — Abrir OS
> Já Completa" mais abaixo.

---

## Passo 1 — Criar Cliente

```bash
curl -s -X POST http://localhost:8080/api/v1/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Silva",
    "documento": "529.982.247-25",
    "email": "joao.silva@email.com",
    "telefone": "(31) 99999-0001",
    "pessoaFisica": true
  }' | jq .
```

**Resultado esperado:** `201 Created`

```json
{
  "clienteId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "nome": "João Silva",
  "documento": "52998224725",
  "email": "joao.silva@email.com",
  "telefone": "(31) 99999-0001",
  "ativo": true
}
```

> ⚙️ **Salve:** `CLIENTE_ID = clienteId`

---

## Passo 2 — Cadastrar Veículo

```bash
curl -s -X POST http://localhost:8080/api/v1/veiculos \
  -H "Content-Type: application/json" \
  -d '{
    "placa": "ABC1D23",
    "modelo": "Onix",
    "marca": "Chevrolet",
    "ano": 2023,
    "clienteId": "{CLIENTE_ID}"
  }' | jq .
```

**Resultado esperado:** `201 Created`

```json
{
  "veiculoId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "placa": "ABC1D23",
  "modelo": "Onix",
  "marca": "Chevrolet",
  "ano": 2023,
  "clienteId": "{CLIENTE_ID}"
}
```

> ⚙️ **Salve:** `VEICULO_ID = veiculoId`

---

## Passo 3 — Adicionar Serviço ao Catálogo

```bash
curl -s -X POST http://localhost:8080/api/v1/servicos \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Troca de Óleo",
    "descricao": "Troca de óleo do motor com filtro incluso",
    "preco": 150.00
  }' | jq .
```

**Resultado esperado:** `201 Created`

```json
{
  "servicoId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "nome": "Troca de Óleo",
  "descricao": "Troca de óleo do motor com filtro incluso",
  "precoBase": 150.00,
  "ativo": true
}
```

> ⚙️ **Salve:** `SERVICO_ID = servicoId`

---

## Passo 4 — Adicionar Peça/Insumo ao Estoque

```bash
curl -s -X POST http://localhost:8080/api/v1/pecas-insumos \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Filtro de Óleo",
    "descricao": "Filtro de óleo para motores flex",
    "preco": 45.90,
    "quantidadeEmEstoque": 0,
    "unidadeDeMedida": "Unidade"
  }' | jq .
```

**Resultado esperado:** `201 Created`

```json
{
  "pecaInsumoId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "nome": "Filtro de Óleo",
  "descricao": "Filtro de óleo para motores flex",
  "precoUnitario": 45.90,
  "quantidadeEstoque": 0,
  "unidadeMedida": "Unidade",
  "ativo": true
}
```

> ⚙️ **Salve:** `PECA_ID = pecaInsumoId`

---

## Passo 5 — Incrementar Estoque da Peça

Adicionamos 10 unidades ao estoque antes de usar na OS.

```bash
curl -s -X PATCH http://localhost:8080/api/v1/pecas-insumos/{PECA_ID}/estoque/entrada \
  -H "Content-Type: application/json" \
  -d '{
    "quantidade": 10
  }' | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "pecaInsumoId": "{PECA_ID}",
  "quantidadeEstoque": 10
}
```

> ✅ Estoque agora tem **10 unidades**. A OS vai consumir 2.

---

## Passo 6 — Criar Ordem de Serviço

```bash
curl -s -X POST http://localhost:8080/api/v1/ordens-servico \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "{CLIENTE_ID}",
    "veiculoId": "{VEICULO_ID}"
  }' | jq .
```

**Resultado esperado:** `201 Created`

```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clienteId": "{CLIENTE_ID}",
  "veiculoId": "{VEICULO_ID}",
  "status": "Recebida",
  "descricaoDiagnostico": null,
  "notificadoEm": null,
  "entregueEm": null,
  "itensServico": [],
  "itensPeca": [],
  "orcamentos": []
}
```

> ⚙️ **Salve:** `OS_ID = id`

---

## Passo 7 — Iniciar Diagnóstico

```bash
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/{OS_ID}/iniciar-diagnostico \
  | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "{OS_ID}",
  "status": "EmDiagnostico",
  ...
}
```

> ✅ OS transitou de `Recebida` → `EmDiagnostico`.

---

## Passo 8 — Registrar Diagnóstico

Esta é a chamada mais importante do fluxo. Ao retornar com sucesso, **três coisas acontecem automaticamente**:
1. O orçamento é criado com `Status = Pendente`
2. O orçamento é enviado ao cliente (`Status = Enviado`), e a OS transita para `AguardandoAprovacao`
3. O integration event `OrcamentoGeradoIntegrationEvent` é publicado, e o módulo PecasInsumos decrementa o estoque

```bash
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/{OS_ID}/registrar-diagnostico \
  -H "Content-Type: application/json" \
  -d '{
    "descricaoDiagnostico": "Motor com desgaste acentuado. Necessário troca de óleo e filtro.",
    "servicos": [
      {
        "servicoId": "{SERVICO_ID}",
        "quantidade": 1
      }
    ],
    "pecas": [
      {
        "pecaInsumoId": "{PECA_ID}",
        "quantidade": 2
      }
    ]
  }' | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "{OS_ID}",
  "status": "EmDiagnostico",
  "descricaoDiagnostico": "Motor com desgaste acentuado. Necessário troca de óleo e filtro.",
  "itensServico": [
    {
      "servicoId": "{SERVICO_ID}",
      "quantidade": 1,
      "precoUnitarioSnapshot": 150.00
    }
  ],
  "itensPeca": [
    {
      "pecaInsumoId": "{PECA_ID}",
      "quantidade": 2,
      "precoUnitarioSnapshot": 45.90
    }
  ],
  "orcamentos": [
    {
      "valorTotal": 241.80,
      "status": "Pendente",
      "dataGeracao": "...",
      "dataEnvio": null,
      "dataAprovacao": null
    }
  ]
}
```

> ⚠️ A resposta ainda mostra `status: "EmDiagnostico"` e orçamento `"Pendente"` porque
> reflete o estado **antes** dos domain event handlers rodarem. Os handlers são chamados
> **após** o SaveChanges. Consulte a OS em seguida para ver o estado atualizado.

---

## Passo 8a — Verificar OS após os handlers automáticos

```bash
curl -s http://localhost:8080/api/v1/ordens-servico/{OS_ID} | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "{OS_ID}",
  "status": "AguardandoAprovacao",
  "descricaoDiagnostico": "Motor com desgaste acentuado. Necessário troca de óleo e filtro.",
  "orcamentos": [
    {
      "valorTotal": 241.80,
      "status": "Enviado",
      "dataGeracao": "...",
      "dataEnvio": "...",
      "dataAprovacao": null
    }
  ]
}
```

> ✅ OS está em `AguardandoAprovacao` e orçamento em `Enviado`.
> Valor total: `1 × R$ 150,00 (serviço) + 2 × R$ 45,90 (peça) = R$ 241,80`

---

## Passo 9 — Verificar Estoque Decrementado

```bash
curl -s http://localhost:8080/api/v1/pecas-insumos/{PECA_ID} | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "pecaInsumoId": "{PECA_ID}",
  "nome": "Filtro de Óleo",
  "quantidadeEstoque": 8
}
```

> ✅ Estoque foi de **10 → 8** (2 unidades consumidas via integration event).
> Isso confirma que o `OrcamentoGeradoIntegrationEvent` foi publicado e consumido
> pelo handler em `PecasInsumos.Application`.

---

## Passo 10 — Aprovar Orçamento

```bash
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/{OS_ID}/aprovar-orcamento \
  | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "{OS_ID}",
  "status": "AguardandoAprovacao",
  "orcamentos": [
    {
      "valorTotal": 241.80,
      "status": "Aprovado",
      "dataAprovacao": "..."
    }
  ]
}
```

> ✅ Orçamento aprovado. `dataAprovacao` preenchida.

---

## Passo 11 — Executar OS

```bash
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/{OS_ID}/executar \
  | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "{OS_ID}",
  "status": "EmExecucao"
}
```

> ✅ OS transitou para `EmExecucao`. Mecânico está trabalhando.

---

## Passo 12 — Finalizar OS

Ao finalizar, o domain event `OrdemServicoFinalizada` é emitido. O handler
`NotificarClienteAoFinalizar` reage e preenche `notificado_em` automaticamente.

```bash
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/{OS_ID}/finalizar \
  | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "{OS_ID}",
  "status": "Finalizada",
  "notificadoEm": null
}
```

> ⚠️ `notificadoEm` pode aparecer `null` na resposta imediata (mesmo motivo do passo 8:
> o handler roda após o SaveChanges). Consulte a OS para confirmar.

---

## Passo 12a — Verificar notificação automática

```bash
curl -s http://localhost:8080/api/v1/ordens-servico/{OS_ID} | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "{OS_ID}",
  "status": "Finalizada",
  "notificadoEm": "2026-05-26T15:30:00Z"
}
```

> ✅ `notificadoEm` preenchido automaticamente pelo handler. Nos logs da aplicação
> deve aparecer a mensagem stub de notificação ao cliente.

---

## Passo 13 — Concluir OS (Entrega do Veículo)

```bash
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/{OS_ID}/concluir \
  | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "{OS_ID}",
  "status": "Entregue",
  "entregueEm": "2026-05-26T15:35:00Z"
}
```

> ✅ OS finalizada com sucesso! Status: `Entregue`.

---

## Passo 14 — Verificar Estado Final Completo

```bash
curl -s http://localhost:8080/api/v1/ordens-servico/{OS_ID} | jq .
```

**Resultado esperado:** `200 OK` — snapshot completo da OS:

```json
{
  "id": "{OS_ID}",
  "clienteId": "{CLIENTE_ID}",
  "veiculoId": "{VEICULO_ID}",
  "status": "Entregue",
  "descricaoDiagnostico": "Motor com desgaste acentuado. Necessário troca de óleo e filtro.",
  "notificadoEm": "2026-05-26T15:30:00Z",
  "entregueEm": "2026-05-26T15:35:00Z",
  "itensServico": [
    {
      "servicoId": "{SERVICO_ID}",
      "quantidade": 1,
      "precoUnitarioSnapshot": 150.00
    }
  ],
  "itensPeca": [
    {
      "pecaInsumoId": "{PECA_ID}",
      "quantidade": 2,
      "precoUnitarioSnapshot": 45.90
    }
  ],
  "orcamentos": [
    {
      "valorTotal": 241.80,
      "status": "Aprovado",
      "dataGeracao": "...",
      "dataEnvio": "...",
      "dataAprovacao": "..."
    }
  ]
}
```

---

## Passo 15 — Consultar OS por Cliente

```bash
curl -s "http://localhost:8080/api/v1/ordens-servico?clienteId={CLIENTE_ID}" | jq .
```

**Resultado esperado:** `200 OK` — lista com a OS criada.

---

## Passo 16 — Consultar Status Resumido da OS

Endpoint leve, retorna apenas `id` e `status` — útil para polling frequente sem trazer o
snapshot completo da OS.

```bash
curl -s http://localhost:8080/api/v1/ordens-servico/{OS_ID}/status | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "{OS_ID}",
  "status": "Entregue"
}
```

---

## Passo 17 — Listar OS para Acompanhamento (painel interno)

Lista as OS "ativas" (exclui `Finalizada` e `Entregue`), ordenadas por prioridade de
status — `EmExecucao` → `AguardandoAprovacao` → `EmDiagnostico` → `Recebida` — e, dentro de
cada status, as mais antigas primeiro. Requer autenticação (`[Authorize]`).

```bash
curl -s http://localhost:8080/api/v1/ordens-servico/acompanhamento | jq .
```

**Resultado esperado:** `200 OK` — lista de OS ativas. Como a OS deste guia já está
`Entregue`, ela **não** deve aparecer nesta lista.

---

## Fluxo Alternativo — Abrir OS Já Completa (`POST /completa`)

Endpoint novo que abre a OS diretamente com serviços e peças definidos, pulando as
etapas manuais de "iniciar diagnóstico" + "registrar diagnóstico" (Passos 7–8). A OS já
nasce em `AguardandoAprovacao`, com orçamento `Enviado` e estoque reservado — mesmos
efeitos colaterais do Passo 8, em uma única chamada.

```bash
curl -s -X POST http://localhost:8080/api/v1/ordens-servico/completa \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "{CLIENTE_ID}",
    "veiculoId": "{VEICULO_ID}",
    "servicos": [
      {
        "servicoId": "{SERVICO_ID}",
        "quantidade": 1
      }
    ],
    "pecas": [
      {
        "pecaInsumoId": "{PECA_ID}",
        "quantidade": 2
      }
    ]
  }' | jq .
```

**Resultado esperado:** `201 Created`

```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clienteId": "{CLIENTE_ID}",
  "veiculoId": "{VEICULO_ID}",
  "status": "AguardandoAprovacao",
  "descricaoDiagnostico": null,
  "itensServico": [
    {
      "servicoId": "{SERVICO_ID}",
      "quantidade": 1,
      "precoUnitarioSnapshot": 150.00
    }
  ],
  "itensPeca": [
    {
      "pecaInsumoId": "{PECA_ID}",
      "quantidade": 2,
      "precoUnitarioSnapshot": 45.90
    }
  ],
  "orcamentos": [
    {
      "valorTotal": 241.80,
      "status": "Enviado",
      "dataGeracao": "...",
      "dataEnvio": "...",
      "dataAprovacao": null
    }
  ]
}
```

> ✅ Diferente do fluxo manual, aqui a OS já nasce em `AguardandoAprovacao` com orçamento
> `Enviado` e `descricaoDiagnostico: null` — não existe etapa de diagnóstico neste fluxo.
> A partir daqui, a OS segue o mesmo ciclo de vida do fluxo principal:
> `aprovar-orcamento → executar → finalizar → concluir` (Passos 10–13).

> ⚙️ **Salve:** `OS_COMPLETA_ID = id`

---

## Checklist de Validação

| # | Verificação | Esperado |
|---|---|---|
| 1 | POST /clientes com CPF válido | 201, clienteId retornado |
| 2 | POST /veiculos com placa Mercosul | 201, veiculoId retornado |
| 3 | POST /servicos | 201, servicoId retornado |
| 4 | POST /pecas-insumos | 201, pecaInsumoId retornado |
| 5 | PATCH estoque/entrada | 200, estoque = 10 |
| 6 | POST /ordens-servico | 201, status = Recebida |
| 7 | PATCH iniciar-diagnostico | 200, status = EmDiagnostico |
| 8 | PATCH registrar-diagnostico | 200, orçamento criado |
| 8a | GET /ordens-servico/{id} | status = AguardandoAprovacao, orçamento = Enviado |
| 9 | GET /pecas-insumos/{id} | estoque = 8 (decrementou 2) |
| 10 | PATCH aprovar-orcamento | 200, orçamento = Aprovado |
| 11 | PATCH executar | 200, status = EmExecucao |
| 12 | PATCH finalizar | 200, status = Finalizada |
| 12a | GET /ordens-servico/{id} | notificadoEm preenchido |
| 13 | PATCH concluir | 200, status = Entregue, entregueEm preenchido |
| 14 | GET /ordens-servico/{id} final | Snapshot completo correto |
| 15 | GET /ordens-servico?clienteId=... | Lista a OS criada |
| 16 | GET /ordens-servico/{id}/status | 200, retorna apenas {id, status} |
| 17 | GET /ordens-servico/acompanhamento | 200, lista OS ativas ordenadas por prioridade, sem Entregue |
| 18 | POST /ordens-servico/completa | 201, status = AguardandoAprovacao direto, orçamento = Enviado, sem diagnóstico |
