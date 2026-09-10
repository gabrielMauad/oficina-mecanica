# ADR-005 — Quatro repositórios e estratégia de branches

- **Status:** Aceita
- **Data:** 2026-09-07

## Contexto

A Fase 3 obriga a organização do projeto em **quatro repositórios separados** — Function Serverless,
infraestrutura Kubernetes (Terraform), infraestrutura do banco gerenciado (Terraform) e aplicação —
cada um com pipeline própria, branch principal protegida e alterações via Pull Request. O enunciado
pede ainda "deploy automatizado para os **ambientes ou branches** de homologação e produção".

O projeto vive hoje em um único repositório, `gabrielMauad/oficina-mecanica`, com o histórico das
Fases 1 e 2 e o usuário `soat-architecture` já adicionado.

O desenvolvimento é conduzido por uma única pessoa, com prazo curto.

## Decisão

**Repositórios.** O repositório atual é **renomeado** para `oficina-mecanica-app`, preservando
histórico, colaboradores e o redirecionamento automático da URL antiga. Os outros três nascem
vazios:

| Repositório | Conteúdo |
|---|---|
| `oficina-mecanica-app` | Aplicação .NET e manifestos Kubernetes |
| `oficina-mecanica-lambda-auth` | Function Serverless de autenticação |
| `oficina-mecanica-infra-k8s` | Terraform do cluster Kubernetes |
| `oficina-mecanica-infra-db` | Terraform do banco de dados gerenciado |

**Branches.** Fluxo único: `feature/*` → Pull Request → `main` → deploy automático.

- `main` protegida em todos os repositórios: sem commit direto, merge apenas por PR, com o job de
  validação como status check obrigatório.
- **Não existe branch de homologação.** A distinção entre ambientes, se houver, será feita **dentro
  da pipeline** (deploy em homologação, validação e promoção para produção), não por branch.

**Ponto deixado em aberto de forma deliberada:** se haverá de fato um ambiente de homologação
provisionado. A decisão depende do crédito disponível na conta AWS Academy, ainda não explorada, e
não bloqueia nada do trabalho local — o modelo de branch é o mesmo nos dois casos.

## Alternativas consideradas

**Promoção entre branches** (`feature` → `homolog` → `main`), com deploy automático em cada uma. É a
leitura mais literal do enunciado e renderia uma demonstração clara de promoção no vídeo.
Descartada pelo atrito: dois Pull Requests por alteração para um desenvolvedor sozinho, com risco de
divergência entre as branches, sem ganho de qualidade proporcional num projeto de poucas semanas.

**Trunk-based com ambientes** (PR para `main`, deploy automático em homologação e promoção manual
para produção via GitHub Environments). É praticamente a decisão tomada, com a diferença de assumir
desde já dois ambientes provisionados. Adiada em vez de descartada — vira essa opção se o orçamento
da conta AWS permitir.

**Manter o nome `oficina-mecanica` para a aplicação.** Evitaria o rename, mas deixaria os quatro
repositórios com nomenclatura assimétrica na lista de links do PDF de entrega.

## Consequências

**Positivas**
- Fluxo de trabalho leve, compatível com o prazo e com uma única pessoa desenvolvendo.
- Histórico das fases anteriores preservado no repositório da aplicação.
- Nomes simétricos, fáceis de apresentar no documento de entrega.

**Negativas e mitigações**
- *O requisito de "branches de homologação e produção" fica atendido apenas pela via de ambientes.*
  O enunciado admite explicitamente "ambientes **ou** branches"; a justificativa é registrada aqui e
  deve ser explicitada no README e no vídeo para não parecer omissão. **Risco residual conhecido:**
  se nenhum ambiente de homologação for provisionado, o item fica descoberto.
- *Quatro repositórios exigem quatro configurações de proteção, secrets e colaborador.* Trabalho
  manual repetido no GitHub, sem automação prevista nesta fase.
- *Dependência entre repositórios* (banco → cluster → lambda/aplicação) deixa de ser garantida pelo
  compilador e passa a depender de ordem de execução documentada, com os outputs compartilhados via
  estado remoto do Terraform.

## Nota de execução (2026-09-10)

Os quatro repositórios foram criados: `oficina-mecanica-app` (rename concluído, histórico e
colaboradores preservados), `oficina-mecanica-lambda-auth` (já existia), e
`oficina-mecanica-infra-k8s` / `oficina-mecanica-infra-db` (criados vazios, com README, `.gitignore`
de Terraform e pipeline de `fmt`/`validate` — sem recursos ainda, pendentes da RFC-002 de escolha de
nuvem). `main` protegida e `soat-architecture` convidado como colaborador nos quatro.

Uma decisão de execução não prevista no texto original: a pasta `infra/` (Terraform do cluster
**kind** local, Fase 2) **não foi movida** para `oficina-mecanica-infra-k8s` neste momento — ela
continua na aplicação porque o job de deploy do `ci-cd.yml` depende dela (`working-directory: infra`)
e `infra/main.tf` aplica os manifestos de `../k8s`, também na aplicação. Mover agora quebraria a
pipeline de deploy sem que o Terraform de nuvem existisse para substituí-la. A migração está registrada
como pendência, a ser feita no mesmo Pull Request que introduzir o Terraform de nuvem em
`oficina-mecanica-infra-k8s`.
