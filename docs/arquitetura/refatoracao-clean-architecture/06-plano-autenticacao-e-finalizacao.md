# Plano 06 — Autenticacao + finalização global

> Ler `00-referencia.md`. Fecha a refatoração: converte o módulo `Autenticacao` (atípico — sem Domain,
> sem Gateway de persistência) e faz a **auditoria final** de toda a solução, garantindo que nada ficou
> para trás.

**Pré-condição:** Planos 02, 03, 04, 05 concluídos (os 3 módulos de negócio prontos).
**Estado final:** os 4 módulos aderentes; solução compila; todos os testes verdes; docs atualizados.

---

## Parte A — Módulo Autenticacao

`Autenticacao` tem só `Application` + `Infrastructure` + `Presentation` (sem Domain/Contracts). Uma
operação: `Login`. Usa `IJwtTokenService` (port em `Application/Services/`, impl `JwtTokenService` em
Infrastructure) e `AdminUserOptions` (config). **Sem agregado, sem repositório, sem ACL.**

### Decisão de aderência
Mesmo sem Domain/Gateway, o módulo recebe **Controller CA + Presenter + Web rename** para não ser uma
exceção visível ao corretor. **Não** criar Gateway de persistência (não há). O `IJwtTokenService`
permanece como **service port** em Application (é a porta para um serviço externo — análogo a um gateway
de saída; manter o nome atual para não inflar).

### Arquivos
- **Criar:** `Autenticacao.Adapters/Autenticacao.Adapters.csproj` (refs: `Autenticacao.Application`, MediatR);
  `Autenticacao.Adapters/Controllers/AutenticacaoController.cs` (CA);
  `Autenticacao.Adapters/Presenters/AutenticacaoPresenter.cs` (`TokenInfo`/`LoginResponse` → `LoginViewModel`);
  `Autenticacao.Adapters/Models/` (Request + `LoginViewModel`, shape do atual `LoginResponse`).
- **Mover/renomear:** `Autenticacao.Presentation` → `Autenticacao.Web`; `AutenticacaoController` (MVC) →
  `AutenticacaoApiController.cs` (fino); `AutenticacaoAssemblyMarker` ajustado.
- **Modificar:** `LoginHandler` — retornar `Result<TokenInfo>` (ou manter `LoginResponse` se preferir,
  desde que a montagem do shape vá para o Presenter); `AutenticacaoModule.cs` (DI: registrar Controller CA +
  Presenter, referenciar `Adapters`); `Program.cs` (`AddApplicationPart` → `Autenticacao.Web`).
- **Deletar:** `LoginResponse` se migrado para ViewModel + Presenter.

### Passos
1. Criar `Autenticacao.Adapters`; ViewModel + Presenter (migrar shape do `LoginResponse`).
2. `LoginHandler` devolve `TokenInfo`; Presenter monta `LoginViewModel`.
3. `AutenticacaoController` (CA): monta `LoginCommand`, `Send`, Presenter.
4. Renomear `Presentation`→`Web`; `AutenticacaoApiController` fino (status idêntico: falha de login → o
   código atual, provavelmente `Unauthorized`/`UnprocessableEntity` — preservar exatamente).
5. DI + `Program.cs`. **Build + testes.**

---

## Parte B — Finalização e auditoria global

### B1. Host e build
- [ ] `Program.cs`: os **4** `AddApplicationPart` apontam para os assemblies `*.Web` renomeados
  (`Autenticacao.Web`, `Cadastro.Web`, `PecasInsumos.Web`, `OrdensServico.Web`).
- [ ] Solução inteira compila sem warnings novos de referência.
- [ ] `.slnx` reflete os novos projetos (`*.Adapters`) e os renomes (`*.Web`).

### B2. Testes
- [ ] Todos os `*.Application.Tests` e `*.Domain.Tests` verdes.
- [ ] `tests/IntegrationTests` verde — **valida que o contrato HTTP de todos os módulos ficou idêntico**.
  Se algum teste de integração quebrar por shape, corrigir a **ViewModel** (não o teste), pois o
  contrato deve ser preservado.

### B3. Auditoria da Regra de Dependência (o trunfo do projeto)
- [ ] Nenhum `*.Adapters.csproj` referencia ASP.NET (`Microsoft.AspNetCore.App`) nem EF/Npgsql.
- [ ] Nenhum `*.Domain.csproj` referencia framework (só `SharedKernel.Domain`).
- [ ] Nenhum `*.Application.csproj` referencia ASP.NET/EF.
- [ ] `*.Web` referencia `*.Adapters` (não mais `*.Application` direto, salvo `Result` via SharedKernel).
- [ ] `Infrastructure` referencia `Adapters` (DI) sem ciclo (Adapters não referencia Infrastructure/Web).
- [ ] Nenhum módulo referencia `Domain`/`Application` de outro módulo (só via `Contracts`).

### B4. Limpeza de código morto
- [ ] Removidos os `*Response` (com `FromX`) substituídos por ViewModel + Presenter, em todos os módulos.
- [ ] Removidas pastas `Models` órfãs em projetos `*.Web` (Requests migrados para `Adapters/Models`).
- [ ] Removidos os antigos `*Adapter` de ACL do OrdensServico (migrados para `*Gateway`).
- [ ] Nenhum `Domain/Ports` remanescente.

### B5. Documentação
- [ ] Atualizar `docs/arquitetura/analise-clean-architecture.md` (ou anexar nota) refletindo o novo
  desenho: Controller CA, Gateways e Presenters como artefatos nomeados; o mapeamento anel→projeto da §2
  de `00-referencia.md`.
- [ ] Atualizar a seção de arquitetura do README, se houver, com o diagrama de projetos por módulo.
- [ ] Registrar no doc a justificativa do MediatR mantido (decisão §3.1) para a banca.

---

## Definition of Done (refatoração inteira)
- [ ] Os 4 módulos seguem o fluxo `ApiController → Controller CA → Send → Handler → Gateway → DataSource`,
      com saída via Presenter.
- [ ] Build da solução + 100% dos testes verdes.
- [ ] Contratos HTTP e cross-module idênticos aos pré-refatoração.
- [ ] Regra de Dependência auditada e intacta (compile-time).
- [ ] Checklist mestre de `00-referencia.md` §8 totalmente marcado.
- [ ] Docs atualizados.
