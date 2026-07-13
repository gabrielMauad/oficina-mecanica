# Guia de Testes E2E — Cenários Alternativos

> **Pré-requisito:** Ter executado pelo menos os passos 1–5 do [`teste-cenario-feliz.md`](teste-cenario-feliz.md)
> para ter `{CLIENTE_ID}`, `{VEICULO_ID}`, `{SERVICO_ID}` e `{PECA_ID}` disponíveis.
> Base URL: `http://localhost:8080`

---

## Índice de Cenários

| # | Cenário | Módulo | HTTP esperado |
|---|---|---|---|
| A1 | CPF inválido no cadastro de cliente | Cadastro | 422 |
| A2 | CNPJ inválido no cadastro de cliente | Cadastro | 422 |
| A3 | Placa fora dos formatos aceitos | Cadastro | 422 |
| A4 | Documento duplicado | Cadastro | 422 |
| A5 | Placa duplicada | Cadastro | 422 |
| A6 | Criar OS com cliente inexistente | OrdemServico | 422 |
| A7 | Criar OS com veículo de outro cliente | OrdemServico | 422 |
| A8 | Registrar diagnóstico com peça sem estoque suficiente | OrdemServico | 422 |
| A9 | Registrar diagnóstico com serviço inexistente | OrdemServico | 422 |
| A10 | Registrar diagnóstico sem itens de peças | OrdemServico | 422 |
| A11 | Transição de estado inválida (pular etapa) | OrdemServico | 422 |
| A12 | Executar OS sem orçamento aprovado | OrdemServico | 422 |
| A13 | **Rejeição de orçamento + estorno de estoque** | OrdemServico + PecasInsumos | 200 + estorno |
| A14 | Concluir OS sem notificação ao cliente | OrdemServico | 422 |
| A15 | Decrementar estoque além do disponível | PecasInsumos | 422 |
| A16 | Buscar entidade inexistente | Todos | 404 |
| A17 | Abrir OS completa (`/completa`) com cliente inexistente | OrdemServico | 422 |
| A18 | Abrir OS completa (`/completa`) sem peças | OrdemServico | 422 |
| A19 | Abrir OS completa (`/completa`) com peça indisponível | OrdemServico + PecasInsumos | 422 |
| A20 | Consultar status (`/{id}/status`) de OS inexistente | OrdemServico | 404 |

---

## A1 — CPF Inválido no Cadastro de Cliente

```bash
curl -s -X POST http://localhost:8080/api/v1/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Fulano Inválido",
    "documento": "111.111.111-11",
    "email": "fulano@email.com",
    "telefone": "(31) 99999-0001",
    "pessoaFisica": true
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "...",
  "description": "CPF inválido."
}
```

> ✅ O value object `Cpf` valida o dígito verificador. Dígitos repetidos (111.111.111-11)
> são inválidos por qualquer algoritmo de CPF.

---

## A2 — CNPJ Inválido no Cadastro de Cliente

```bash
curl -s -X POST http://localhost:8080/api/v1/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Empresa Inválida Ltda",
    "documento": "11.111.111/0001-11",
    "email": "empresa@email.com",
    "telefone": "(31) 3333-0001",
    "pessoaFisica": false
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "...",
  "description": "CNPJ inválido."
}
```

---

## A3 — Placa Fora dos Formatos Aceitos

Testa uma placa que não é nem Mercosul (`ABC1D23`) nem padrão antigo (`ABC-1234`).

```bash
curl -s -X POST http://localhost:8080/api/v1/veiculos \
  -H "Content-Type: application/json" \
  -d '{
    "placa": "PLACA99",
    "modelo": "Gol",
    "marca": "Volkswagen",
    "ano": 2020,
    "clienteId": "{CLIENTE_ID}"
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "...",
  "description": "Placa inválida."
}
```

> ✅ Formatos válidos: `ABC1D23` (Mercosul) ou `ABC-1234` (antigo). `PLACA99` não casa com nenhum.

---

## A4 — Documento Duplicado (Cliente)

Tenta cadastrar um segundo cliente com o mesmo CPF do passo 1 do cenário feliz.

```bash
curl -s -X POST http://localhost:8080/api/v1/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Silva Cópia",
    "documento": "529.982.247-25",
    "email": "joao.copia@email.com",
    "telefone": "(31) 99999-0002",
    "pessoaFisica": true
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "Cliente.DocumentoJaExiste",
  "description": "Já existe um cliente com este documento."
}
```

---

## A5 — Placa Duplicada (Veículo)

Tenta cadastrar um segundo veículo com a mesma placa `ABC1D23` do cenário feliz.

```bash
curl -s -X POST http://localhost:8080/api/v1/veiculos \
  -H "Content-Type: application/json" \
  -d '{
    "placa": "ABC1D23",
    "modelo": "HB20",
    "marca": "Hyundai",
    "ano": 2022,
    "clienteId": "{CLIENTE_ID}"
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "Veiculo.PlacaJaExiste",
  "description": "Já existe um veículo com esta placa."
}
```

---

## A6 — Criar OS com Cliente Inexistente

```bash
curl -s -X POST http://localhost:8080/api/v1/ordens-servico \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "00000000-0000-0000-0000-000000000001",
    "veiculoId": "{VEICULO_ID}"
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "OrdemServico.ClienteInexistenteOuInativo",
  "description": "O cliente informado não existe ou está inativo."
}
```

> ✅ O ACL adapter `ClienteInfoAdapter` verifica se o cliente existe e está ativo
> antes de criar a OS.

---

## A7 — Criar OS com Veículo que Não Pertence ao Cliente

Crie um segundo cliente e tente abrir OS usando o veículo do primeiro cliente.

**7a — Criar segundo cliente:**
```bash
curl -s -X POST http://localhost:8080/api/v1/clientes \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Maria Souza",
    "documento": "275.484.936-08",
    "email": "maria.souza@email.com",
    "telefone": "(31) 99999-0099",
    "pessoaFisica": true
  }' | jq '.clienteId'
```

> ⚙️ **Salve:** `CLIENTE2_ID = clienteId`

**7b — Tentar criar OS com veículo do cliente 1:**
```bash
curl -s -X POST http://localhost:8080/api/v1/ordens-servico \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "{CLIENTE2_ID}",
    "veiculoId": "{VEICULO_ID}"
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "OrdemServico.VeiculoInexistenteOuNaoPertenceAoCliente",
  "description": "O veículo informado não existe ou não pertence ao cliente."
}
```

---

## A8 — Registrar Diagnóstico com Peça Sem Estoque Suficiente

**Pré-requisito:** Criar uma nova OS (passo 6 do cenário feliz) e iniciá-la (passo 7).
Depois crie uma peça com estoque zero e tente usá-la no diagnóstico.

**8a — Criar peça com estoque zero:**
```bash
curl -s -X POST http://localhost:8080/api/v1/pecas-insumos \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Pastilha de Freio",
    "descricao": "Pastilha dianteira",
    "preco": 89.90,
    "quantidadeEmEstoque": 0,
    "unidadeDeMedida": "Par"
  }' | jq '.pecaInsumoId'
```

> ⚙️ **Salve:** `PECA_SEM_ESTOQUE_ID = pecaInsumoId`

**8b — Criar e iniciar nova OS:**
```bash
OS2_ID=$(curl -s -X POST http://localhost:8080/api/v1/ordens-servico \
  -H "Content-Type: application/json" \
  -d '{"clienteId": "{CLIENTE_ID}", "veiculoId": "{VEICULO_ID}"}' \
  | jq -r '.id')

curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS2_ID/iniciar-diagnostico
```

**8c — Tentar registrar diagnóstico com peça sem estoque:**
```bash
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS2_ID/registrar-diagnostico \
  -H "Content-Type: application/json" \
  -d '{
    "descricaoDiagnostico": "Freios desgastados.",
    "servicos": [
      { "servicoId": "{SERVICO_ID}", "quantidade": 1 }
    ],
    "pecas": [
      { "pecaInsumoId": "{PECA_SEM_ESTOQUE_ID}", "quantidade": 2 }
    ]
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "OrdemServico.PecaIndisponivel",
  "description": "Peça/insumo indisponível para a quantidade informada."
}
```

> ✅ A verificação de disponibilidade via `IPecaDisponibilidadePort` (ACL) ocorre antes
> de tocar o agregado. Nenhuma OS foi modificada.

---

## A9 — Registrar Diagnóstico com Serviço Inexistente

```bash
# Usando a OS2 do cenário anterior (já em EmDiagnostico)
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS2_ID/registrar-diagnostico \
  -H "Content-Type: application/json" \
  -d '{
    "descricaoDiagnostico": "Diagnóstico teste.",
    "servicos": [
      { "servicoId": "00000000-0000-0000-0000-000000000099", "quantidade": 1 }
    ],
    "pecas": [
      { "pecaInsumoId": "{PECA_ID}", "quantidade": 1 }
    ]
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "OrdemServico.ServicoNaoEncontrado",
  "description": "Servico não encontrado."
}
```

---

## A10 — Registrar Diagnóstico Sem Itens de Peças

O domínio exige pelo menos 1 serviço e 1 peça para gerar orçamento.

```bash
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS2_ID/registrar-diagnostico \
  -H "Content-Type: application/json" \
  -d '{
    "descricaoDiagnostico": "Apenas serviço, sem peças.",
    "servicos": [
      { "servicoId": "{SERVICO_ID}", "quantidade": 1 }
    ],
    "pecas": []
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "OrdemServico.OrcamentoSemPecas",
  "description": "Não é possível gerar orçamento sem itens de peças."
}
```

---

## A11 — Transição de Estado Inválida (Pular Etapa)

Tenta aprovar orçamento numa OS que ainda está em `Recebida` (sem passar pelo diagnóstico).

```bash
# Criar OS nova — ainda em Recebida
OS3_ID=$(curl -s -X POST http://localhost:8080/api/v1/ordens-servico \
  -H "Content-Type: application/json" \
  -d '{"clienteId": "{CLIENTE_ID}", "veiculoId": "{VEICULO_ID}"}' \
  | jq -r '.id')

# Tentar aprovar sem passar pelas etapas anteriores
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS3_ID/aprovar-orcamento \
  | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "OrdemServico.TransicaoInvalida",
  "description": "Ordem de Serviço só pode aprovar orçamento quando está Aguardando Aprovação."
}
```

> ✅ Testes adicionais de transição inválida:
> - `PATCH /executar` em status `Recebida` → 422
> - `PATCH /finalizar` em status `AguardandoAprovacao` → 422
> - `PATCH /concluir` em status `EmExecucao` (sem finalizar) → 422

---

## A12 — Executar OS com Orçamento Aprovado Ausente

O domínio exige orçamento com status `Aprovado` para transitar para `EmExecucao`.
Tenta executar uma OS em `AguardandoAprovacao` sem aprovar o orçamento primeiro.

```bash
# Use OS3_ID do cenário A11
# Leve a OS até AguardandoAprovacao normalmente:
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS3_ID/iniciar-diagnostico

curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS3_ID/registrar-diagnostico \
  -H "Content-Type: application/json" \
  -d '{
    "descricaoDiagnostico": "Teste de transição.",
    "servicos": [{ "servicoId": "{SERVICO_ID}", "quantidade": 1 }],
    "pecas": [{ "pecaInsumoId": "{PECA_ID}", "quantidade": 1 }]
  }'

# (aguardar GET para confirmar AguardandoAprovacao, depois tentar executar sem aprovar)
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS3_ID/executar | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "OrdemServico.TransicaoInvalida",
  "description": "Ordem de Serviço só pode iniciar execução quando o orçamento está Aprovado."
}
```

---

## A13 — Rejeição de Orçamento com Estorno de Estoque ⭐

**Este é o cenário alternativo mais importante.** Valida a cadeia completa de
eventos de rejeição: `RejeitarOrcamento` → domain event `OrcamentoRejeitado` →
integration event `OrcamentoRejeitadoIntegrationEvent` → estorno em PecasInsumos.

### Preparação — verificar estoque antes

```bash
curl -s http://localhost:8080/api/v1/pecas-insumos/{PECA_ID} | jq '.quantidadeEstoque'
```

> 📝 Anote o valor atual (deve ser **8** se veio do cenário feliz, ou **10** se é início).

### 13a — Criar nova OS e levá-la até AguardandoAprovacao

```bash
# Criar OS
OS_REJEICAO_ID=$(curl -s -X POST http://localhost:8080/api/v1/ordens-servico \
  -H "Content-Type: application/json" \
  -d '{"clienteId": "{CLIENTE_ID}", "veiculoId": "{VEICULO_ID}"}' \
  | jq -r '.id')

echo "OS de rejeição: $OS_REJEICAO_ID"

# Iniciar diagnóstico
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS_REJEICAO_ID/iniciar-diagnostico

# Registrar diagnóstico com 3 peças
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS_REJEICAO_ID/registrar-diagnostico \
  -H "Content-Type: application/json" \
  -d '{
    "descricaoDiagnostico": "Revisão geral — cliente vai avaliar orçamento.",
    "servicos": [
      { "servicoId": "{SERVICO_ID}", "quantidade": 1 }
    ],
    "pecas": [
      { "pecaInsumoId": "{PECA_ID}", "quantidade": 3 }
    ]
  }' | jq '{status, orcamentos}'
```

### 13b — Verificar estoque decrementado (antes da rejeição)

```bash
curl -s http://localhost:8080/api/v1/pecas-insumos/{PECA_ID} | jq '.quantidadeEstoque'
```

> ✅ Estoque deve ter diminuído **3 unidades** (decremento automático via integration event).

### 13c — Rejeitar orçamento

```bash
curl -s -X PATCH http://localhost:8080/api/v1/ordens-servico/$OS_REJEICAO_ID/rejeitar-orcamento \
  | jq .
```

**Resultado esperado:** `200 OK`

```json
{
  "id": "...",
  "status": "AguardandoAprovacao",
  "orcamentos": [
    {
      "status": "Rejeitado"
    }
  ]
}
```

### 13d — Verificar estoque estornado (após a rejeição)

```bash
curl -s http://localhost:8080/api/v1/pecas-insumos/{PECA_ID} | jq '.quantidadeEstoque'
```

**Resultado esperado:** estoque voltou ao valor anterior à geração do orçamento.

> ✅ O domain event `OrcamentoRejeitado` → integration event
> `OrcamentoRejeitadoIntegrationEvent` → handler `IncrementarEstoqueQuandoOrcamentoRejeitado`
> devolveu as 3 unidades ao estoque.

### 13e — Verificar OS após rejeição

```bash
curl -s http://localhost:8080/api/v1/ordens-servico/$OS_REJEICAO_ID | jq .
```

> ✅ Status permanece `AguardandoAprovacao`. O orçamento rejeitado permite
> reabrir o diagnóstico (novo `registrar-diagnostico` é aceito pelo domínio).

---

## A14 — Concluir OS Sem Notificação ao Cliente

O domínio exige `notificado_em != null` para permitir a conclusão.
Tenta concluir uma OS que está `Finalizada` mas cujo `NotificarCliente` ainda não rodou.

> **Nota:** No fluxo atual, `NotificarCliente` é chamado automaticamente pelo handler
> `NotificarClienteAoFinalizar` logo após `Finalizar`. Este cenário seria acionado
> apenas se o handler falhasse. Para simulá-lo diretamente, pode-se:
>
> 1. Verificar que `GET /ordens-servico/{id}` mostra `notificadoEm != null` após `finalizar`
> 2. Confirmar que a regra existe no domínio pelo teste unitário (ver `OrdemServico.cs`)

A regra de negócio no domínio (`Concluir`) é:

```csharp
if (Status != StatusOrdemServico.Finalizada || NotificadoEm == null)
    return Error.Validation("OrdemServico.TransicaoInvalida",
        "Ordem de Serviço só pode ser concluída quando está Finalizada e o cliente foi notificado.");
```

---

## A15 — Decrementar Estoque Além do Disponível

```bash
# Tenta decrementar mais do que há em estoque
curl -s -X PATCH http://localhost:8080/api/v1/pecas-insumos/{PECA_ID}/estoque/saida \
  -H "Content-Type: application/json" \
  -d '{
    "quantidade": 9999
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "PecaInsumo.EstoqueInsuficiente",
  "description": "Quantidade em estoque insuficiente para a operação."
}
```

> ✅ A regra de negócio do agregado `PecaInsumo` impede estoque negativo.

---

## A16 — Buscar Entidades Inexistentes (404)

**Cliente inexistente:**
```bash
curl -s http://localhost:8080/api/v1/clientes/00000000-0000-0000-0000-000000000099 | jq .
# Esperado: 404
```

**Veículo inexistente:**
```bash
curl -s http://localhost:8080/api/v1/veiculos/00000000-0000-0000-0000-000000000099 | jq .
# Esperado: 404
```

**Serviço inexistente:**
```bash
curl -s http://localhost:8080/api/v1/servicos/00000000-0000-0000-0000-000000000099 | jq .
# Esperado: 404
```

**Peça/Insumo inexistente:**
```bash
curl -s http://localhost:8080/api/v1/pecas-insumos/00000000-0000-0000-0000-000000000099 | jq .
# Esperado: 404
```

**OS inexistente:**
```bash
curl -s http://localhost:8080/api/v1/ordens-servico/00000000-0000-0000-0000-000000000099 | jq .
# Esperado: 404
```

---

## A17 — Abrir OS Completa com Cliente Inexistente

O endpoint `POST /completa` roda as mesmas validações de ACL do endpoint `Gerar` antes
de montar o orçamento.

```bash
curl -s -X POST http://localhost:8080/api/v1/ordens-servico/completa \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "00000000-0000-0000-0000-000000000001",
    "veiculoId": "{VEICULO_ID}",
    "servicos": [
      { "servicoId": "{SERVICO_ID}", "quantidade": 1 }
    ],
    "pecas": [
      { "pecaInsumoId": "{PECA_ID}", "quantidade": 1 }
    ]
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "OrdemServico.ClienteInexistenteOuInativo",
  "description": "O cliente informado não existe ou está inativo."
}
```

> ✅ Nenhuma OS é criada — a validação de cliente ocorre antes de montar orçamento ou
> reservar estoque.

---

## A18 — Abrir OS Completa Sem Peças

Diferente do fluxo manual (`registrar-diagnostico`), aqui a ausência de peças é barrada
já no `AbrirOrdemServicoCompletaValidator` (FluentValidation), antes mesmo de chegar ao
domínio.

```bash
curl -s -X POST http://localhost:8080/api/v1/ordens-servico/completa \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "{CLIENTE_ID}",
    "veiculoId": "{VEICULO_ID}",
    "servicos": [
      { "servicoId": "{SERVICO_ID}", "quantidade": 1 }
    ],
    "pecas": []
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "validation.failed",
  "description": "Ao menos uma peça deve ser informada."
}
```

> ✅ O mesmo vale para `servicos: []` (mensagem "Ao menos um serviço deve ser
> informado."), `servicoId`/`pecaInsumoId` vazios e `quantidade <= 0` em qualquer item.

---

## A19 — Abrir OS Completa com Peça Indisponível

```bash
# Criar peça com estoque zero
PECA_SEM_ESTOQUE_ID=$(curl -s -X POST http://localhost:8080/api/v1/pecas-insumos \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Amortecedor Traseiro",
    "descricao": "Amortecedor sem estoque",
    "preco": 210.00,
    "quantidadeEmEstoque": 0,
    "unidadeDeMedida": "Unidade"
  }' | jq -r '.pecaInsumoId')

curl -s -X POST http://localhost:8080/api/v1/ordens-servico/completa \
  -H "Content-Type: application/json" \
  -d '{
    "clienteId": "{CLIENTE_ID}",
    "veiculoId": "{VEICULO_ID}",
    "servicos": [
      { "servicoId": "{SERVICO_ID}", "quantidade": 1 }
    ],
    "pecas": [
      { "pecaInsumoId": "'"$PECA_SEM_ESTOQUE_ID"'", "quantidade": 1 }
    ]
  }' | jq .
```

**Resultado esperado:** `422 Unprocessable Entity`

```json
{
  "code": "OrdemServico.PecaIndisponivel",
  "description": "Peça/insumo indisponível para a quantidade informada."
}
```

> ✅ Nenhuma OS é criada e nenhum estoque é reservado — a verificação de disponibilidade
> via `IPecaDisponibilidadeGateway` ocorre antes de chamar `OrdemServico.AbrirComServicos`.
> Se o `pecaInsumoId` não existir de fato, o código retornado é
> `OrdemServico.PecaNaoEncontrada` em vez de `PecaIndisponivel`.

---

## A20 — Consultar Status de OS Inexistente

```bash
curl -s http://localhost:8080/api/v1/ordens-servico/00000000-0000-0000-0000-000000000099/status \
  | jq .
# Esperado: 404
```

> ✅ `GET /{id}/status` reaproveita a mesma query `ObterOrdemServicoPorIdQuery` do
> `GET /{id}`, então o comportamento de 404 para OS inexistente é idêntico.

---

## Resumo de Respostas de Erro

| Situação | HTTP Status | Quando ocorre |
|---|---|---|
| Dado de entrada inválido (VO falha) | `422` | CPF/CNPJ/placa inválidos, campo obrigatório ausente |
| Regra de negócio violada | `422` | Transição inválida, estoque insuficiente, orçamento existente |
| Conflito de unicidade | `422` | Documento duplicado, placa duplicada |
| Recurso não encontrado | `404` | GET/DELETE de entidade inexistente |
| ACL: entidade cross-BC não existe | `422` | Cliente/veículo inexistente ao criar OS |
| Erro inesperado | `500` | Problem Details (RFC 7807) via middleware global |
