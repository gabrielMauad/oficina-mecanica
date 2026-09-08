# ADR-003 — Dois emissores de token e autorização por papel

- **Status:** Aceita
- **Data:** 2026-09-07
- **Contexto maior:** [RFC-001 — Estratégia de autenticação](../rfcs/001-estrategia-de-autenticacao.md)

## Contexto

A Fase 3 exige autenticação do **cliente** por CPF. Mas as rotas protegidas do sistema são, em sua
maioria, operações **da oficina**: cadastrar cliente, cadastrar veículo, dar entrada de peça, abrir
ordem de serviço, registrar diagnóstico. O cliente, no fluxo modelado nas fases anteriores, faz
apenas duas coisas — acompanha a OS e aprova ou recusa o orçamento.

Até a Fase 2 existia um único fluxo de autenticação (email e senha de administrador) e um único
nível de acesso: qualquer autenticado acessava qualquer rota `[Authorize]`.

Se a autenticação por CPF simplesmente substituísse a existente, qualquer cliente cadastrado
passaria a ter acesso total ao sistema — inclusive ao estoque e ao cadastro de outros clientes.

## Decisão

Os dois fluxos de autenticação **coexistem**, e a autorização passa a considerar o **papel**:

| Fluxo | Emissor | Credencial | Papel no token |
|---|---|---|---|
| Cliente | Function Serverless | CPF | `Cliente` |
| Oficina | Aplicação (rota de login existente) | email + senha | `Oficina` |

- Ambos os tokens são assinados com o mesmo segredo (ver [ADR-001](001-jwt-hs256-segredo-compartilhado.md))
  e distinguidos pela claim de emissor e pela claim de papel.
- As rotas deixam de exigir apenas "estar autenticado" e passam a exigir o papel adequado.
- O acompanhamento da OS (`GET /api/v1/ordens-servico/acompanhamento`) e a aprovação ou recusa do
  orçamento passam a exigir o **token de CPF**, atendendo diretamente ao requisito de proteger
  rotas sensíveis com autenticação via CPF. As demais rotas exigem o token da oficina.

O contrato de claims e o mapa de rotas por papel estão no
[RFC-001](../rfcs/001-estrategia-de-autenticacao.md).

## Alternativas consideradas

**Apenas CPF, sem distinção de papel.** Seria a leitura mais literal da spec e o menor esforço.
Descartada porque qualquer cliente ativo teria acesso administrativo ao sistema, sem nenhuma
barreira — e porque o fluxo de abertura de OS, que o próprio enunciado manda diagramar, deixaria de
ter um operador da oficina.

**Apenas CPF, com o papel vindo do banco** (um registro marcado como funcionário). Manteria um único
emissor e ainda separaria papéis. Descartada porque no fluxo por CPF não existe segredo: o acesso
administrativo passaria a depender apenas do conhecimento de um CPF, que é um identificador
público. Trocar uma falha por outra pior.

## Consequências

**Positivas**
- O modelo de acesso passa a refletir o domínio: cliente e oficina são atores distintos.
- Dá conteúdo real aos diagramas de sequência exigidos (dois atores, dois fluxos de entrada).

**Negativas e mitigações**
- *É o item mais pesado da evolução da aplicação nesta fase*: exige claim de papel nos dois
  emissores, decisão de autorização rota a rota e ajuste dos testes de integração, que hoje
  autenticam tudo com o usuário administrador. Mitigado tratando o mapa de rotas como parte do
  RFC-001, decidido antes de escrever código.
- *A aplicação passa a aceitar tokens de dois emissores.* Com segredo compartilhado é apenas
  configuração (`ValidIssuers` com dois valores), mas é um ponto a acertar e testar.
- *Pode ser questionado por não ser "tudo por CPF".* A justificativa formal está no RFC-001:
  funcionário não é cliente e não deve ser autenticado por um identificador público.
