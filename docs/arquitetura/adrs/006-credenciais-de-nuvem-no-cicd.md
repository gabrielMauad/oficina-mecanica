# ADR-006 — Credenciais de nuvem no CI/CD

- **Status:** Aceita
- **Data:** 2026-09-10
- **Contexto maior:** [RFC-002 — Escolha do provedor de nuvem](../rfcs/002-escolha-do-provedor-de-nuvem.md)

## Contexto

Os quatro repositórios do projeto ([ADR-005](005-quatro-repositorios-e-estrategia-de-branches.md))
precisam de pipelines que provisionem infraestrutura e publiquem a aplicação na AWS. Para isso, o
GitHub Actions precisa se autenticar na conta.

A prática recomendada hoje é **OIDC**: o provedor de identidade do GitHub é registrado na conta AWS,
um IAM role é criado com uma política de confiança que aceita tokens daquele repositório, e o
workflow assume esse role. Nenhuma credencial de longa duração é armazenada. Era o que o plano
original da fase previa.

Só que a conta disponível é uma **AWS Academy Learner Lab**, e ela **não permite criar IAM roles**
(apenas *service-linked roles*) nem usuários IAM — ver RFC-002 §6.1. Sem poder criar o role, não há
como estabelecer a relação de confiança que o OIDC exige. O caminho recomendado está fechado.

O que a conta oferece são **credenciais temporárias de sessão** — `AWS_ACCESS_KEY_ID`,
`AWS_SECRET_ACCESS_KEY` e `AWS_SESSION_TOKEN` — renovadas a cada sessão do laboratório.

## Decisão

As pipelines autenticam na AWS usando as **credenciais temporárias da sessão**, armazenadas como
**secrets do repositório** no GitHub e atualizadas manualmente a cada nova sessão do laboratório.

- Os três valores (incluindo obrigatoriamente o **`AWS_SESSION_TOKEN`**) são configurados como
  secrets em cada repositório que faz deploy.
- Os workflows os consomem via `aws-actions/configure-aws-credentials`, que aceita o token de sessão.
- **Nenhuma credencial é commitada**, em nenhuma hipótese, nem mesmo em exemplo.
- Os jobs de deploy permanecem acionáveis manualmente (`workflow_dispatch`) além do gatilho
  automático, para permitir reexecução depois de renovar os secrets sem precisar de um commit novo.

## Alternativas consideradas

**OIDC com IAM role dedicado.** É a prática correta e era o plano. **Tecnicamente impossível** nesta
conta: exige criar um IAM role, o que a Academy não permite. Não foi descartada por preferência —
foi descartada por indisponibilidade.

**Usuário IAM com access key de longa duração.** Removeria a renovação manual. Também
**impossível**: a conta não permite criar usuários IAM.

**Provisionar tudo manualmente pelo console, sem pipeline de deploy.** Eliminaria o problema, mas
descumpre o requisito explícito da fase de deploy automatizado e de pipelines funcionais. Descartada.

**Conta AWS pessoal, custeada do próprio bolso.** Restauraria o OIDC, ao custo de pagar o control
plane do EKS, nós, NAT Gateway e RDS. Descartada no RFC-002 por custo.

## Consequências

**Positivas**
- As pipelines funcionam de verdade contra a nuvem, com deploy automatizado, atendendo ao requisito.
- Nenhuma credencial de longa duração existe — as da sessão expiram sozinhas, o que é, isoladamente,
  melhor do que uma access key permanente esquecida num secret.

**Negativas e mitigações**
- **O deploy automático só funciona enquanto a sessão do laboratório estiver ativa.** Expirada a
  sessão, o job falha na autenticação até que os secrets sejam renovados. É uma limitação da conta,
  não do desenho: deve ser declarada no README dos repositórios que fazem deploy e mencionada na
  demonstração em vídeo, para não ser lida como pipeline quebrada.
- **Renovação manual recorrente.** Passo operacional a executar antes de qualquer sessão de trabalho
  ou gravação. Mitigado pelo `workflow_dispatch`, que permite reexecutar sem novo commit.
- **Credenciais válidas trafegam para os secrets do GitHub.** Mitigado pelo prazo curto de validade e
  por manter os repositórios sob controle de um único titular.

## Nota sobre a migração futura

Numa conta AWS comum, esta decisão é revertida com um esforço pequeno e localizado: cria-se o
provedor OIDC e o role, troca-se o passo de credenciais nos workflows, e removem-se os três secrets.
Nada além dos arquivos de workflow muda — nenhum módulo Terraform e nenhum código de aplicação.
