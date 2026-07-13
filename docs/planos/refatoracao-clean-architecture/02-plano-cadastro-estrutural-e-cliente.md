# Plano 02 — Cadastro: estrutural + fluxo Cliente

> Ler `00-referencia.md` e usar o `PecasInsumos` (Plano 01) como template concreto. Este plano monta a
> estrutura do módulo `Cadastro` (projeto `Adapters`, `Web`, DI, gateways de persistência dos 3 agregados)
> e implementa o **fluxo completo do agregado Cliente**. Os agregados Servico e Veiculo ficam para o Plano 03.

**Pré-condição:** Plano 01 concluído.
**Estado final:** estrutura do Cadastro pronta; Cliente 100% no novo fluxo; build + testes verdes. Servico
e Veiculo **ainda no fluxo antigo** (MVC → Send direto) — estado híbrido válido e compilável.

---

## Escopo

`Cadastro` tem 3 agregados: **Cliente** (`IClienteRepository`), **Servico** (`IServicoRepository`),
**Veiculo** (`IVeiculoRepository`). **Sem ACL consumindo outros módulos.**

Agregado **Cliente** — ops: `CadastrarCliente`, `AtualizarCliente`, `DesativarCliente` (commands);
`ObterClientePorId`, `ListarClientes` (queries). Controller `ClientesController`. Models:
`AtualizarNomeRequest`, `AtualizarTelefoneRequest`.

### Fora de escopo
- **Fluxo (Presenter/ViewModel/Controller CA/retorno de entidade/slim MVC) de Servico e Veiculo** → Plano 03.
- `Contracts` (`ICadastroClienteQuery` etc.) — inalterado.

> ⚠️ **Restrição (DataSource no anel verde — ver `00-referencia §3.2`):** `Application` não pode referenciar
> `Adapters`, então **nenhum handler pode injetar `I{Entity}Repository`** — todo handler de persistência usa
> `I{X}Gateway`. Por isso este plano cria os **3** DataSources (→ Adapters) e os **3** Gateways, e religa os
> handlers dos **3** agregados ao gateway. Só o **Cliente** ganha o fluxo completo (retorno de entidade +
> Presenter); Servico/Veiculo usam gateway mas seguem retornando `*Response` até o Plano 03.

---

## Arquivos

### Criar
- `Cadastro.Adapters/Cadastro.Adapters.csproj` (refs: `Cadastro.Application`, `Cadastro.Domain`, MediatR).
- `Cadastro.Adapters/Controllers/ClienteController.cs` (CA).
- `Cadastro.Adapters/Presenters/ClientePresenter.cs`.
- `Cadastro.Adapters/Gateways/{Cliente,Servico,Veiculo}Gateway.cs` (`: I{X}Gateway`) — **os 3** (finos, delegam ao repo).
- `Cadastro.Adapters/Models/` — Requests + ViewModels do Cliente.
- `Cadastro.Application/Gateways/I{Cliente,Servico,Veiculo}Gateway.cs` — **as 3** interfaces de gateway.

### Mover / renomear
- `Cadastro.Domain/Cliente/IClienteRepository.cs` → `Cadastro.Adapters/DataSources/IClienteRepository.cs`.
- `Cadastro.Domain/Servico/IServicoRepository.cs` → `Cadastro.Adapters/DataSources/IServicoRepository.cs`.
- `Cadastro.Domain/Veiculo/IVeiculoRepository.cs` → `Cadastro.Adapters/DataSources/IVeiculoRepository.cs`.
- `Cadastro.Presentation/` → **`Cadastro.Web`** (csproj, pasta, `CadastroAssemblyMarker`).
- `Cadastro.Presentation/Controllers/ClientesController.cs` → `Cadastro.Web/Controllers/ClienteApiController.cs`.
- Requests HTTP do Cliente → `Cadastro.Adapters/Models/`.

### Modificar
- Handlers do **Cliente** (5): injeção → `IClienteGateway`; commands retornam `Result<Cliente>`; remover `FromCliente`/montagem inline.
- Handlers de **Servico/Veiculo**: trocar injeção `I{Entity}Repository` → `I{Entity}Gateway`. **Mantêm o retorno `*Response`/DTO atual** (Presenter/entidade só no Plano 03).
- `Cadastro.Infrastructure/Persistence/Repositories/{Cliente,Servico,Veiculo}Repository.cs` — atualizar `using` das interfaces (agora em `Cadastro.Adapters.DataSources`; o Infra já referencia `Adapters`).
- `Cadastro.Infrastructure/CadastroModule.cs` — referenciar `Cadastro.Adapters`; registrar **os 3** `I{X}Gateway`→`{X}Gateway`, `ClienteController` (CA), `ClientePresenter`.
- `src/Bootstrap/Api/Program.cs` — `AddApplicationPart(typeof(CadastroAssemblyMarker).Assembly)` → assembly `Cadastro.Web`.
- Solução: adicionar `Cadastro.Adapters`, renomear `Presentation`→`Web`.

### Deletar (ao final)
- `CadastrarClienteResponse`/`AtualizarClienteResponse`/`DesativarClienteResponse`/`ObterClientePorIdResponse` (migrados para ViewModel + Presenter), se não mais referenciados.

---

## Passos ordenados
1. Criar `Cadastro.Adapters.csproj`; adicionar à solução.
2. Mover os **3** `I*Repository` para `Adapters/DataSources/`; ajustar usings nos repos (Infra). Criar as **3** `I{X}Gateway` (App) + os **3** `{X}Gateway` (Adapters, delegam ao repo).
3. Religar **todos** os handlers (Cliente/Servico/Veiculo) ao seu `I{X}Gateway` (Servico/Veiculo ainda retornando `*Response`). Registrar os 3 gateways no DI. **Build** — verde esperado (comportamento inalterado).
4. ViewModels + `ClientePresenter` (migrar `FromCliente`); handlers de comando do Cliente → `Result<Cliente>`.
5. `ClienteController` (CA): Command, `Send`, Presenter.
6. Renomear `Presentation`→`Web`; `ClienteApiController` fino (status por endpoint idêntico ao atual — atenção: `ObterPorId`/`Listar`/`Desativar`→`NotFound`, `Criar`/`AtualizarX`→`UnprocessableEntity`, `Desativar`→`NoContent`).
7. DI restante (`ClienteController`/`ClientePresenter`) + `Program.cs`.
8. **Build solução + testes.** Remover código morto do Cliente.

> ⚠️ Após o passo 7, os MVC controllers de **Servico e Veiculo** ainda existem em `Cadastro.Web` no
> formato antigo (injetam `ISender`, `Send` direto). Isso é esperado e compila. Eles serão convertidos no
> Plano 03.

## Testes
- `Cadastro.Application.Tests` — **todos** os handlers dos 3 agregados agora dependem de `I{X}Gateway`, então os testes que mockavam `I{Entity}Repository` passam a mockar o `I{X}Gateway` correspondente (Cliente/Servico/Veiculo). Ajustar asserts dos handlers de Cliente (agora devolvem `Cliente`); os de Servico/Veiculo seguem assertando o `*Response` (fluxo antigo).
- `Cadastro.Domain.Tests` — inalterado.
- Integração — endpoints de Cliente com JSON idêntico.

## Definition of Done
- [ ] `Cadastro.Adapters` + `Cadastro.Web` criados; build verde.
- [ ] 3 DataSources movidos para `Adapters/DataSources/`; **3 Gateways criados**; **todos** os handlers (Cliente/Servico/Veiculo) usando gateway (nenhum handler injeta `I{Entity}Repository`).
- [ ] Cliente 100% no novo fluxo; Servico/Veiculo com gateway porém ainda retornando `*Response` (fluxo antigo).
- [ ] Testes verdes; contrato HTTP do Cliente idêntico; código morto do Cliente removido.
