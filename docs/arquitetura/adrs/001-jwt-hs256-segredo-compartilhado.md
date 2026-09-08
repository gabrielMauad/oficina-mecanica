# ADR-001 — JWT assinado em HS256 com segredo compartilhado

- **Status:** Aceita
- **Data:** 2026-09-07
- **Contexto maior:** [RFC-001 — Estratégia de autenticação](../rfcs/001-estrategia-de-autenticacao.md)

## Contexto

Até a Fase 2, a aplicação era ao mesmo tempo emissora e validadora do JWT: o
`JwtTokenService` assinava e o `Program.cs` validava, ambos com o mesmo
`SymmetricSecurityKey` vindo de configuração. Emissor e validador eram o mesmo processo, então a
chave nunca precisou atravessar fronteira.

Na Fase 3 isso muda: a Function Serverless passa a **emitir** o token e a aplicação em Kubernetes
passa a apenas **validá-lo**. São dois componentes, em repositórios e runtimes distintos. É preciso
decidir como eles combinam a chave.

Há uma restrição relevante do lado da infraestrutura: o *JWT authorizer* nativo do API Gateway HTTP
API da AWS só valida tokens de um emissor OIDC, que publica suas chaves públicas em um endpoint de
descoberta. Ele **não** aceita assinatura simétrica.

## Decisão

O JWT é assinado em **HS256 com um único segredo compartilhado** entre a Lambda e a aplicação.

- O segredo é injetado por variável de ambiente nos dois componentes; a origem será o AWS Secrets
  Manager quando a infraestrutura existir, e um valor fixo de desenvolvimento até lá.
- A aplicação passa a validar **issuer e audience** (hoje ambos estão desligados), aceitando os
  emissores definidos no RFC-001.
- A validação do token é responsabilidade da **aplicação**. Se for desejável validar também na
  borda, será por um *Lambda authorizer* próprio — não pelo authorizer nativo.

## Alternativas consideradas

**RS256 com par de chaves e JWKS.** A Lambda assinaria com a chave privada e publicaria a pública
em `/.well-known/jwks.json`, junto de um `openid-configuration`. É o padrão de mercado, impede que
quem valida forje tokens, e destravaria o JWT authorizer nativo do API Gateway.

Foi descartada por custo e por risco de cronograma: exige gerar e guardar o par de chaves, expor
dois endpoints públicos novos, e trocar a validação da aplicação por busca e cache de JWKS. Como a
conta AWS disponível é uma AWS Academy, ainda não explorada, optou-se por reduzir o número de
elementos que dependem dela.

## Consequências

**Positivas**
- A mudança na aplicação é mínima — a infraestrutura de validação simétrica já existe.
- Nenhum endpoint novo precisa ser exposto publicamente.
- A autenticação pode ser desenvolvida e testada localmente sem nenhum recurso de nuvem.

**Negativas e mitigações**
- *Quem valida também consegue assinar.* Aceitável no escopo deste projeto, em que os dois
  componentes pertencem ao mesmo domínio de confiança. Mitigado guardando o segredo no Secrets
  Manager e nunca no repositório.
- *O JWT authorizer nativo do API Gateway fica indisponível.* O gateway continua responsável por
  roteamento e controle, como a spec exige; a verificação de identidade fica na aplicação, ou em um
  Lambda authorizer se houver tempo.
- *Rotacionar o segredo exige coordenar dois componentes.* Documentado como procedimento manual;
  não há rotação automática nesta fase.

## Migração futura

Trocar para RS256 afeta apenas a assinatura na Lambda e a configuração de validação na aplicação —
o contrato de claims do RFC-001 permanece válido. É uma decisão reversível a custo moderado.
