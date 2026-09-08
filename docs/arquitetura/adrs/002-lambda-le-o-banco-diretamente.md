# ADR-002 — A Lambda de autenticação lê o banco diretamente

- **Status:** Aceita
- **Data:** 2026-09-07
- **Contexto maior:** [RFC-001 — Estratégia de autenticação](../rfcs/001-estrategia-de-autenticacao.md)

## Contexto

A Function Serverless precisa, a cada tentativa de login, saber se existe um cliente com o CPF
informado e se ele está ativo. Esse dado vive em `cadastro.cliente`, tabela pertencente ao bounded
context **Cadastro**, cujo schema é criado e evoluído pelas migrations do EF Core da aplicação.

O projeto adota, desde a Fase 2, isolamento explícito entre contextos — um schema por módulo e
nenhuma FK entre schemas — justamente para simular fronteiras de microsserviço. Um componente
externo lendo a tabela de um contexto atravessa essa fronteira.

## Decisão

A Lambda **consulta o banco diretamente**, com um `SELECT` sobre `cadastro.cliente` filtrando por
documento e situação ativa.

- O acesso usa um **usuário de banco dedicado, com permissão apenas de `SELECT`** nessa tabela.
- A Lambda **não escreve** no banco e **não executa migrations** — o schema continua com um dono
  único, que é a aplicação.
- A dependência com o schema é protegida por um **teste de integração da Lambda** contra um
  PostgreSQL com as migrations da aplicação aplicadas, de modo que uma renomeação de coluna quebre
  o build da Lambda em vez do login em produção.

Esta é uma **exceção deliberada** ao isolamento entre contextos, restrita ao fluxo de autenticação.

## Alternativas consideradas

**A Lambda consulta um endpoint interno da aplicação.** Manteria o conhecimento do schema num lugar
só e a regra de "cliente válido" dentro do Cadastro. Foi descartada por três motivos: cria
dependência circular conceitual (o autenticador passa a depender do serviço que ele protege),
derruba o login sempre que a aplicação estiver indisponível, e exige expor uma rota sem
autenticação — abrindo, no componente protegido, o tipo de superfície que a fase quer fechar. Além
disso, é mais trabalho: exige endpoint novo na aplicação **e** cliente HTTP na Lambda, contra apenas
uma consulta na opção escolhida.

**A Lambda repassa a chamada para a rota de login da aplicação.** Seria o menor esforço de todos e
reaproveitaria o `LoginHandler` existente. Descartada por aderência: a spec atribui à Function
Serverless a responsabilidade de validar o CPF, consultar a base **e** gerar o token. Nesse desenho
ela não faria nenhuma das três — seria um repasse removível sem alterar o comportamento do sistema.

## Consequências

**Positivas**
- O login não depende da aplicação estar no ar.
- Sem dependência circular entre o autenticador e o serviço autenticado.
- Um hop de rede a menos e menos modos de falha (sem timeout de HTTP no caminho crítico do login).

**Negativas e mitigações**
- *Duas bases de código conhecem o mesmo schema.* Mitigado pelo teste de integração descrito acima
  e pela restrição a uma única tabela e duas colunas.
- *Fere o isolamento entre contextos que o projeto defende.* Aceito conscientemente e limitado ao
  fluxo de autenticação, que é um caso reconhecido de exceção — autenticadores de mercado
  (Cognito, Keycloak) leem seu próprio store diretamente.
- *A Lambda precisa estar na VPC para alcançar o banco gerenciado*, o que aumenta o tempo de
  inicialização a frio. Impacto restrito ao login e tratado na fase de infraestrutura.
