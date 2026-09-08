# ADR-004 — Correlação de requisições via `traceId` do W3C/OpenTelemetry

- **Status:** Aceita
- **Data:** 2026-09-07

## Contexto

A Fase 3 exige logs estruturados em JSON com mecanismo de correlação (`correlationId` ou `traceId`),
de modo a permitir acompanhar uma requisição enquanto ela atravessa os componentes da arquitetura:
API Gateway, Function Serverless, aplicação em Kubernetes e banco. O vídeo de entrega precisa
demonstrar logs estruturados, correlação e traces em execução.

Hoje a aplicação usa o logging padrão do ASP.NET, em texto e sem nenhum identificador de correlação.

## Decisão

A correlação usa o **`trace id` do padrão W3C Trace Context**, propagado pelo header `traceparent` e
gerado pela instrumentação OpenTelemetry. **Não** haverá um `correlationId` próprio.

Decisões que acompanham e sustentam essa escolha:

- **Todos os componentes são instrumentados**, incluindo a Function Serverless — não apenas a
  aplicação. É o que elimina a lacuna de correlação na borda do login.
- **Amostragem em 100%** nos ambientes deste projeto, para que nenhum trace usado como evidência
  seja descartado.
- Todo log é enriquecido com `trace_id` e `span_id` a partir do `Activity.Current`, ligando cada
  linha de log ao trace correspondente.
- A verificação local não depende de ferramenta paga: os traces podem ser exportados via OTLP para
  um coletor ou Jaeger no `docker-compose` durante o desenvolvimento.

## Alternativas consideradas

**Um `X-Correlation-Id` próprio.** Um middleware geraria ou aproveitaria o header e carimbaria os
logs. Vantagem: independe de APM, de amostragem e de instrumentação, e pode ser devolvido ao cliente
na resposta. Descartada por ser código proprietário para resolver um problema que a instrumentação
ponta a ponta já resolve, e por não conectar log a trace — perdendo o item "traces em execução"
exigido no vídeo.

**Os dois identificadores em paralelo**, com o `traceId` como identificador técnico e o
`correlationId` como identificador de negócio resiliente. Descartada por redundância: as lacunas que
justificariam o segundo identificador (instrumentação parcial, amostragem agressiva) foram fechadas
pelas decisões acima, e manter dois ids exigiria explicar a diferença sem ganho prático.

## Consequências

**Positivas**
- Correlação e propagação vêm da instrumentação, sem código de header próprio para manter.
- Permite navegar de um log de erro direto para o trace completo da requisição no APM, com o tempo
  gasto em cada componente — a demonstração mais forte possível para o vídeo.
- Adere a um padrão de mercado em vez de a uma convenção interna.

**Negativas e mitigações**
- *Depende de instrumentação em todos os pontos.* Mitigado por decisão explícita de instrumentar
  também a Lambda, e não apenas a aplicação.
- *Amostragem poderia descartar evidências.* Mitigado fixando a amostragem em 100% neste projeto —
  aceitável pelo volume, mas é uma configuração que não se levaria a um ambiente de produção real.
- *O identificador é opaco e não escolhido por nós*, o que dificulta usá-lo como protocolo de
  atendimento ao usuário final. Irrelevante no escopo atual.
