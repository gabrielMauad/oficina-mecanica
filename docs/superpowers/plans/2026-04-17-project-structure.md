# Project Structure — Oficina Mecânica Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Criar todos os 25 projetos do Modular Monolith com pastas, `.csproj` registrados na solução `OficinaMecanica.slnx`. Referências entre projetos serão adicionadas manualmente ao longo do desenvolvimento.

**Architecture:** Modular Monolith com 3 Bounded Contexts (Cadastro, OrdemServico, PecasInsumos), cada um com 5 projetos por camada (Domain, Application, Infrastructure, Presentation, Contracts) + SharedKernel (2 projetos) + Bootstrap/Api (1) + testes (7).

**Tech Stack:** .NET 10, C# class libraries, ASP.NET Core Web API (Bootstrap), xUnit (testes)

---

## File Map

| Projeto | Tipo | Path |
|---|---|---|
| SharedKernel.Domain | classlib | `src/SharedKernel/SharedKernel.Domain/` |
| SharedKernel.Application | classlib | `src/SharedKernel/SharedKernel.Application/` |
| Cadastro.Domain | classlib | `src/Modules/Cadastro/Cadastro.Domain/` |
| Cadastro.Application | classlib | `src/Modules/Cadastro/Cadastro.Application/` |
| Cadastro.Infrastructure | classlib | `src/Modules/Cadastro/Cadastro.Infrastructure/` |
| Cadastro.Presentation | classlib | `src/Modules/Cadastro/Cadastro.Presentation/` |
| Cadastro.Contracts | classlib | `src/Modules/Cadastro/Cadastro.Contracts/` |
| OrdemServico.Domain | classlib | `src/Modules/OrdemServico/OrdemServico.Domain/` |
| OrdemServico.Application | classlib | `src/Modules/OrdemServico/OrdemServico.Application/` |
| OrdemServico.Infrastructure | classlib | `src/Modules/OrdemServico/OrdemServico.Infrastructure/` |
| OrdemServico.Presentation | classlib | `src/Modules/OrdemServico/OrdemServico.Presentation/` |
| OrdemServico.Contracts | classlib | `src/Modules/OrdemServico/OrdemServico.Contracts/` |
| PecasInsumos.Domain | classlib | `src/Modules/PecasInsumos/PecasInsumos.Domain/` |
| PecasInsumos.Application | classlib | `src/Modules/PecasInsumos/PecasInsumos.Application/` |
| PecasInsumos.Infrastructure | classlib | `src/Modules/PecasInsumos/PecasInsumos.Infrastructure/` |
| PecasInsumos.Presentation | classlib | `src/Modules/PecasInsumos/PecasInsumos.Presentation/` |
| PecasInsumos.Contracts | classlib | `src/Modules/PecasInsumos/PecasInsumos.Contracts/` |
| Api | webapi | `src/Bootstrap/Api/` |
| Cadastro.Domain.Tests | xunit | `tests/Modules/Cadastro/Cadastro.Domain.Tests/` |
| Cadastro.Application.Tests | xunit | `tests/Modules/Cadastro/Cadastro.Application.Tests/` |
| OrdemServico.Domain.Tests | xunit | `tests/Modules/OrdemServico/OrdemServico.Domain.Tests/` |
| OrdemServico.Application.Tests | xunit | `tests/Modules/OrdemServico/OrdemServico.Application.Tests/` |
| PecasInsumos.Domain.Tests | xunit | `tests/Modules/PecasInsumos/PecasInsumos.Domain.Tests/` |
| PecasInsumos.Application.Tests | xunit | `tests/Modules/PecasInsumos/PecasInsumos.Application.Tests/` |
| IntegrationTests | xunit | `tests/IntegrationTests/` |

---

## Referências de projeto (regras da arquitetura — gabarito para validação ao final)

```
SharedKernel.Domain        →  (nenhuma)
SharedKernel.Application   →  SharedKernel.Domain

X.Contracts                →  SharedKernel.Domain
X.Domain                   →  SharedKernel.Domain
X.Application              →  X.Domain, SharedKernel.Domain, SharedKernel.Application
X.Infrastructure           →  X.Application, X.Contracts, SharedKernel.Domain, SharedKernel.Application
X.Presentation             →  X.Application, SharedKernel.Domain, SharedKernel.Application

Bootstrap/Api              →  todos os X.Presentation, todos os X.Infrastructure,
                               SharedKernel.Domain, SharedKernel.Application

X.Domain.Tests             →  X.Domain
X.Application.Tests        →  X.Application
IntegrationTests           →  Api (Bootstrap)
```

Referências cross-module (ex.: OrdemServico.Application → Cadastro.Contracts) serão adicionadas quando as features que as exigem forem implementadas (YAGNI).

---

## Task 1: SharedKernel

**Files:**
- Create: `src/SharedKernel/SharedKernel.Domain/SharedKernel.Domain.csproj`
- Create: `src/SharedKernel/SharedKernel.Application/SharedKernel.Application.csproj`

- [ ] **Step 1: Criar projetos SharedKernel**

```bash
cd "C:/Users/gabri/Projetos/Pós Tech/Tech Challenge/oficina-mecanica-v2"

dotnet new classlib -n SharedKernel.Domain --framework net10.0 -o src/SharedKernel/SharedKernel.Domain
dotnet new classlib -n SharedKernel.Application --framework net10.0 -o src/SharedKernel/SharedKernel.Application
```

- [ ] **Step 2: Remover os arquivos Class1.cs gerados**

```bash
rm src/SharedKernel/SharedKernel.Domain/Class1.cs
rm src/SharedKernel/SharedKernel.Application/Class1.cs
```

---

## Task 2: Módulo Cadastro

**Files:**
- Create: `src/Modules/Cadastro/Cadastro.Contracts/Cadastro.Contracts.csproj`
- Create: `src/Modules/Cadastro/Cadastro.Domain/Cadastro.Domain.csproj`
- Create: `src/Modules/Cadastro/Cadastro.Application/Cadastro.Application.csproj`
- Create: `src/Modules/Cadastro/Cadastro.Infrastructure/Cadastro.Infrastructure.csproj`
- Create: `src/Modules/Cadastro/Cadastro.Presentation/Cadastro.Presentation.csproj`

- [ ] **Step 1: Criar os 5 projetos do módulo Cadastro**

```bash
cd "C:/Users/gabri/Projetos/Pós Tech/Tech Challenge/oficina-mecanica-v2"

dotnet new classlib -n Cadastro.Contracts      --framework net10.0 -o src/Modules/Cadastro/Cadastro.Contracts
dotnet new classlib -n Cadastro.Domain         --framework net10.0 -o src/Modules/Cadastro/Cadastro.Domain
dotnet new classlib -n Cadastro.Application    --framework net10.0 -o src/Modules/Cadastro/Cadastro.Application
dotnet new classlib -n Cadastro.Infrastructure --framework net10.0 -o src/Modules/Cadastro/Cadastro.Infrastructure
dotnet new classlib -n Cadastro.Presentation   --framework net10.0 -o src/Modules/Cadastro/Cadastro.Presentation
```

- [ ] **Step 2: Remover Class1.cs gerados**

```bash
rm src/Modules/Cadastro/Cadastro.Contracts/Class1.cs
rm src/Modules/Cadastro/Cadastro.Domain/Class1.cs
rm src/Modules/Cadastro/Cadastro.Application/Class1.cs
rm src/Modules/Cadastro/Cadastro.Infrastructure/Class1.cs
rm src/Modules/Cadastro/Cadastro.Presentation/Class1.cs
```

---

## Task 3: Módulo OrdemServico

**Files:**
- Create: `src/Modules/OrdemServico/OrdemServico.Contracts/OrdemServico.Contracts.csproj`
- Create: `src/Modules/OrdemServico/OrdemServico.Domain/OrdemServico.Domain.csproj`
- Create: `src/Modules/OrdemServico/OrdemServico.Application/OrdemServico.Application.csproj`
- Create: `src/Modules/OrdemServico/OrdemServico.Infrastructure/OrdemServico.Infrastructure.csproj`
- Create: `src/Modules/OrdemServico/OrdemServico.Presentation/OrdemServico.Presentation.csproj`

- [ ] **Step 1: Criar os 5 projetos do módulo OrdemServico**

```bash
cd "C:/Users/gabri/Projetos/Pós Tech/Tech Challenge/oficina-mecanica-v2"

dotnet new classlib -n OrdemServico.Contracts      --framework net10.0 -o src/Modules/OrdemServico/OrdemServico.Contracts
dotnet new classlib -n OrdemServico.Domain         --framework net10.0 -o src/Modules/OrdemServico/OrdemServico.Domain
dotnet new classlib -n OrdemServico.Application    --framework net10.0 -o src/Modules/OrdemServico/OrdemServico.Application
dotnet new classlib -n OrdemServico.Infrastructure --framework net10.0 -o src/Modules/OrdemServico/OrdemServico.Infrastructure
dotnet new classlib -n OrdemServico.Presentation   --framework net10.0 -o src/Modules/OrdemServico/OrdemServico.Presentation
```

- [ ] **Step 2: Remover Class1.cs gerados**

```bash
rm src/Modules/OrdemServico/OrdemServico.Contracts/Class1.cs
rm src/Modules/OrdemServico/OrdemServico.Domain/Class1.cs
rm src/Modules/OrdemServico/OrdemServico.Application/Class1.cs
rm src/Modules/OrdemServico/OrdemServico.Infrastructure/Class1.cs
rm src/Modules/OrdemServico/OrdemServico.Presentation/Class1.cs
```

---

## Task 4: Módulo PecasInsumos

**Files:**
- Create: `src/Modules/PecasInsumos/PecasInsumos.Contracts/PecasInsumos.Contracts.csproj`
- Create: `src/Modules/PecasInsumos/PecasInsumos.Domain/PecasInsumos.Domain.csproj`
- Create: `src/Modules/PecasInsumos/PecasInsumos.Application/PecasInsumos.Application.csproj`
- Create: `src/Modules/PecasInsumos/PecasInsumos.Infrastructure/PecasInsumos.Infrastructure.csproj`
- Create: `src/Modules/PecasInsumos/PecasInsumos.Presentation/PecasInsumos.Presentation.csproj`

- [ ] **Step 1: Criar os 5 projetos do módulo PecasInsumos**

```bash
cd "C:/Users/gabri/Projetos/Pós Tech/Tech Challenge/oficina-mecanica-v2"

dotnet new classlib -n PecasInsumos.Contracts      --framework net10.0 -o src/Modules/PecasInsumos/PecasInsumos.Contracts
dotnet new classlib -n PecasInsumos.Domain         --framework net10.0 -o src/Modules/PecasInsumos/PecasInsumos.Domain
dotnet new classlib -n PecasInsumos.Application    --framework net10.0 -o src/Modules/PecasInsumos/PecasInsumos.Application
dotnet new classlib -n PecasInsumos.Infrastructure --framework net10.0 -o src/Modules/PecasInsumos/PecasInsumos.Infrastructure
dotnet new classlib -n PecasInsumos.Presentation   --framework net10.0 -o src/Modules/PecasInsumos/PecasInsumos.Presentation
```

- [ ] **Step 2: Remover Class1.cs gerados**

```bash
rm src/Modules/PecasInsumos/PecasInsumos.Contracts/Class1.cs
rm src/Modules/PecasInsumos/PecasInsumos.Domain/Class1.cs
rm src/Modules/PecasInsumos/PecasInsumos.Application/Class1.cs
rm src/Modules/PecasInsumos/PecasInsumos.Infrastructure/Class1.cs
rm src/Modules/PecasInsumos/PecasInsumos.Presentation/Class1.cs
```

---

## Task 5: Bootstrap/Api

**Files:**
- Create: `src/Bootstrap/Api/Api.csproj`

- [ ] **Step 1: Criar o projeto Web API**

```bash
cd "C:/Users/gabri/Projetos/Pós Tech/Tech Challenge/oficina-mecanica-v2"

dotnet new webapi -n Api --framework net10.0 --no-openapi -o src/Bootstrap/Api
```

> `--no-openapi` porque o Swagger/OpenAPI será configurado manualmente conforme a arquitetura (ver seção 9 do doc).

---

## Task 6: Projetos de Teste

**Files:**
- Create: `tests/Modules/Cadastro/Cadastro.Domain.Tests/Cadastro.Domain.Tests.csproj`
- Create: `tests/Modules/Cadastro/Cadastro.Application.Tests/Cadastro.Application.Tests.csproj`
- Create: `tests/Modules/OrdemServico/OrdemServico.Domain.Tests/OrdemServico.Domain.Tests.csproj`
- Create: `tests/Modules/OrdemServico/OrdemServico.Application.Tests/OrdemServico.Application.Tests.csproj`
- Create: `tests/Modules/PecasInsumos/PecasInsumos.Domain.Tests/PecasInsumos.Domain.Tests.csproj`
- Create: `tests/Modules/PecasInsumos/PecasInsumos.Application.Tests/PecasInsumos.Application.Tests.csproj`
- Create: `tests/IntegrationTests/IntegrationTests.csproj`

- [ ] **Step 1: Criar os 6 projetos de testes unitários**

```bash
cd "C:/Users/gabri/Projetos/Pós Tech/Tech Challenge/oficina-mecanica-v2"

dotnet new xunit -n Cadastro.Domain.Tests      --framework net10.0 -o tests/Modules/Cadastro/Cadastro.Domain.Tests
dotnet new xunit -n Cadastro.Application.Tests --framework net10.0 -o tests/Modules/Cadastro/Cadastro.Application.Tests

dotnet new xunit -n OrdemServico.Domain.Tests      --framework net10.0 -o tests/Modules/OrdemServico/OrdemServico.Domain.Tests
dotnet new xunit -n OrdemServico.Application.Tests --framework net10.0 -o tests/Modules/OrdemServico/OrdemServico.Application.Tests

dotnet new xunit -n PecasInsumos.Domain.Tests      --framework net10.0 -o tests/Modules/PecasInsumos/PecasInsumos.Domain.Tests
dotnet new xunit -n PecasInsumos.Application.Tests --framework net10.0 -o tests/Modules/PecasInsumos/PecasInsumos.Application.Tests
```

- [ ] **Step 2: Criar o projeto de testes de integração**

```bash
dotnet new xunit -n IntegrationTests --framework net10.0 -o tests/IntegrationTests
```

- [ ] **Step 3: Remover arquivos UnitTest1.cs gerados**

```bash
rm tests/Modules/Cadastro/Cadastro.Domain.Tests/UnitTest1.cs
rm tests/Modules/Cadastro/Cadastro.Application.Tests/UnitTest1.cs
rm tests/Modules/OrdemServico/OrdemServico.Domain.Tests/UnitTest1.cs
rm tests/Modules/OrdemServico/OrdemServico.Application.Tests/UnitTest1.cs
rm tests/Modules/PecasInsumos/PecasInsumos.Domain.Tests/UnitTest1.cs
rm tests/Modules/PecasInsumos/PecasInsumos.Application.Tests/UnitTest1.cs
rm tests/IntegrationTests/UnitTest1.cs
```

---

## Task 7: Registrar todos os projetos na solução e verificar

**Files:**
- Modify: `OficinaMecanica.slnx`

- [ ] **Step 1: Adicionar todos os projetos à solução**

```bash
cd "C:/Users/gabri/Projetos/Pós Tech/Tech Challenge/oficina-mecanica-v2"

dotnet sln OficinaMecanica.slnx add \
  src/SharedKernel/SharedKernel.Domain/SharedKernel.Domain.csproj \
  src/SharedKernel/SharedKernel.Application/SharedKernel.Application.csproj \
  src/Modules/Cadastro/Cadastro.Contracts/Cadastro.Contracts.csproj \
  src/Modules/Cadastro/Cadastro.Domain/Cadastro.Domain.csproj \
  src/Modules/Cadastro/Cadastro.Application/Cadastro.Application.csproj \
  src/Modules/Cadastro/Cadastro.Infrastructure/Cadastro.Infrastructure.csproj \
  src/Modules/Cadastro/Cadastro.Presentation/Cadastro.Presentation.csproj \
  src/Modules/OrdemServico/OrdemServico.Contracts/OrdemServico.Contracts.csproj \
  src/Modules/OrdemServico/OrdemServico.Domain/OrdemServico.Domain.csproj \
  src/Modules/OrdemServico/OrdemServico.Application/OrdemServico.Application.csproj \
  src/Modules/OrdemServico/OrdemServico.Infrastructure/OrdemServico.Infrastructure.csproj \
  src/Modules/OrdemServico/OrdemServico.Presentation/OrdemServico.Presentation.csproj \
  src/Modules/PecasInsumos/PecasInsumos.Contracts/PecasInsumos.Contracts.csproj \
  src/Modules/PecasInsumos/PecasInsumos.Domain/PecasInsumos.Domain.csproj \
  src/Modules/PecasInsumos/PecasInsumos.Application/PecasInsumos.Application.csproj \
  src/Modules/PecasInsumos/PecasInsumos.Infrastructure/PecasInsumos.Infrastructure.csproj \
  src/Modules/PecasInsumos/PecasInsumos.Presentation/PecasInsumos.Presentation.csproj \
  src/Bootstrap/Api/Api.csproj \
  tests/Modules/Cadastro/Cadastro.Domain.Tests/Cadastro.Domain.Tests.csproj \
  tests/Modules/Cadastro/Cadastro.Application.Tests/Cadastro.Application.Tests.csproj \
  tests/Modules/OrdemServico/OrdemServico.Domain.Tests/OrdemServico.Domain.Tests.csproj \
  tests/Modules/OrdemServico/OrdemServico.Application.Tests/OrdemServico.Application.Tests.csproj \
  tests/Modules/PecasInsumos/PecasInsumos.Domain.Tests/PecasInsumos.Domain.Tests.csproj \
  tests/Modules/PecasInsumos/PecasInsumos.Application.Tests/PecasInsumos.Application.Tests.csproj \
  tests/IntegrationTests/IntegrationTests.csproj
```

Expected: 25 linhas "Project `...` added to the solution."

- [ ] **Step 2: Verificar que a solução compila sem erros**

```bash
dotnet build OficinaMecanica.slnx
```

Expected: `Build succeeded.` com 0 erros. Warnings de projetos vazios são esperados e aceitáveis.

- [ ] **Step 3: Commit**

```bash
git add src/ tests/ OficinaMecanica.slnx
git commit -m "feat: scaffold 25-project modular monolith structure

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```
