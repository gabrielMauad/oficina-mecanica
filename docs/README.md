# Documentação — Sistema de Oficina Mecânica

Índice da documentação do projeto. O ponto de entrada geral é o
[README principal](../README.md); esta pasta detalha cada tema.

## 🏛️ Arquitetura — [`arquitetura/`](arquitetura/)

| Documento | Conteúdo |
|---|---|
| [`diagramas/componentes.md`](arquitetura/diagramas/componentes.md) | **Desenho dos componentes** (C4 níveis 1–3, Mermaid) |
| [`diagramas/infraestrutura.md`](arquitetura/diagramas/infraestrutura.md) | **Desenho da infraestrutura** provisionada (cluster kind, Mermaid) |
| [`diagramas/fluxo-deploy.md`](arquitetura/diagramas/fluxo-deploy.md) | **Desenho do fluxo de deploy** (CI/CD, Mermaid) |
| [`diagramas/sequencia-autenticacao.md`](arquitetura/diagramas/sequencia-autenticacao.md) | **Sequência de autenticação** — cliente por CPF e oficina por email/senha (Mermaid) |
| [`estrutura-do-projeto.md`](arquitetura/estrutura-do-projeto.md) | Decisões de estrutura, papel de cada projeto, regras de referência |
| [`clean-architecture.md`](arquitetura/clean-architecture.md) | Análise de aderência à Clean Architecture (anéis, Regra de Dependência) |
| [`rfcs/`](arquitetura/rfcs/) | **RFCs** — análise das decisões técnicas relevantes, com alternativas e trade-offs |
| [`adrs/`](arquitetura/adrs/) | **ADRs** — registro das decisões arquiteturais permanentes |
| [`decisoes.md`](arquitetura/decisoes.md) | Decisões de design relevantes + ciclo de vida da OS |
| [`metricas.md`](arquitetura/metricas.md) | Contrato de métricas de negócio (meters, instrumentos, dashboards, limitações) |
| [`database-schema.md`](arquitetura/database-schema.md) | Schema do banco com DDL, diagrama ER (Mermaid) e rastreabilidade event storming → coluna |
| [`event-storming.md`](arquitetura/event-storming.md) | Event storming com todos os fluxos e contextos delimitados |

## 📋 Planos — [`planos/`](planos/)

Planos de implementação e specs de refatoração (histórico de execução do projeto).

| Documento | Conteúdo |
|---|---|
| [`plano-de-tarefas.md`](planos/plano-de-tarefas.md) | Plano de tarefas do MVP (T01–T32) |
| [`refatoracao-clean-architecture/`](planos/refatoracao-clean-architecture/) | Planos da refatoração para Clean Architecture (00–07) |
| [`refatoracao-domain-events.md`](planos/refatoracao-domain-events.md) | Spec da refatoração dos domain events |
| [`infra-fase-2/`](planos/infra-fase-2/) | Plano da infraestrutura da Fase 2 (Docker, K8s, Terraform, CI/CD) |
| [`fase-3/`](planos/fase-3/) | Análise da spec da Fase 3 e divisão do trabalho |

## 🧪 Guias — [`guias/`](guias/)

| Documento | Conteúdo |
|---|---|
| [`teste-cenario-feliz.md`](guias/teste-cenario-feliz.md) | Guia E2E do happy path (curl + resultados esperados) |
| [`teste-cenarios-alternativos.md`](guias/teste-cenarios-alternativos.md) | Guia E2E de validações de erro e fluxo de rejeição/estorno |
| [`collection_bruno.yml`](guias/collection_bruno.yml) | Coleção [Bruno](https://usebruno.com) com todos os endpoints |

## 📎 Referência

| Pasta / Documento | Conteúdo |
|---|---|
| [`spec/`](spec/) | Enunciados oficiais FIAP/SOAT (PDFs das Fases 1, 2 e 3) |
| [`../infra/README.md`](../infra/README.md) | Documentação dos recursos Terraform e passo a passo de apply/destroy |
| [`images/`](images/) | Imagens (relatórios de cobertura) |
