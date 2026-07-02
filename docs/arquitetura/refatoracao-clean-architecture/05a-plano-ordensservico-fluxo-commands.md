# Plano 05a — OrdensServico: fluxo dos commands

> Ler `00-referencia.md`. Implementa o fluxo verde dos **8 commands** de OrdensServico sobre a estrutura
> do Plano 04. Cria o Controller CA, o Presenter e as ViewModels (artefatos compartilhados também pelas
> queries do 05b) e faz os 8 commands. As 2 queries permanecem no fluxo antigo — estado híbrido válido.

**Pré-condição:** Plano 04 concluído (gateways + ports + projeto `Adapters` + `Web` prontos).
**Estado final:** 8 commands no novo fluxo; 2 queries ainda em `Send` direto; build + testes verdes; JSON idêntico.

---

## Escopo (8 commands)

`GerarOrdemServico`, `IniciarDiagnostico`, `RegistrarDiagnostico`, `AprovarOrcamento`,
`RejeitarOrcamento`, `ExecutarOrdemServico`, `FinalizarOrdemServico`, `ConcluirOrdemServico`.

### Fora de escopo
- 2 queries (`ObterOrdemServicoPorId`, `ListarOrdensPorCliente`) → **Plano 05b** (permanecem no fluxo antigo aqui).
- Gateways/ports/projeto (Plano 04). `DomainEventHandlers/` e `Contracts` inalterados.

---

## Arquivos

### Criar
- `OrdensServico.Adapters/Controllers/OrdemServicoController.cs` (CA) — **com os 8 métodos de command** (as 2 queries entram no 05b).
- `OrdensServico.Adapters/Presenters/OrdemServicoPresenter.cs` — `Present(OrdemServico)` → ViewModel (absorve a montagem inline hoje no `RegistrarDiagnosticoHandler` e equivalentes). É o Presenter que o 05b reutiliza.
- `OrdensServico.Adapters/Models/` — ViewModel (shape = `OrdemServicoResumoDto` atual, p/ preservar JSON) + Request Models dos commands.

### Mover / renomear
- `OrdensServico.Web/Controllers/OrdensServicoController.cs` → `OrdemServicoApiController.cs` (MVC). Neste plano: **slim apenas dos 8 endpoints de command**; os 2 endpoints de query continuam com `Send` direto.
- `OrdensServico.Web/Models/RegistrarDiagnosticoRequest.cs` → `OrdensServico.Adapters/Models/` (record framework-free).

### Modificar
- **8 handlers de comando:** retorno `Result<OrdemServicoResumoDto>` → `Result<OrdemServico>`; **remover a montagem inline do DTO** (migra para o Presenter).
- `OrdensServico.Infrastructure/OrdemServicoModule.cs` — registrar `OrdemServicoController` (CA) + `OrdemServicoPresenter`.

> ⚠️ **`OrdemServicoResumoDto` (Contracts) NÃO é deletado.** Continua sendo o retorno do
> `IOrdemServicoResumoQuery` (caminho cross-module, impl em `Infrastructure/Queries/`). A ViewModel nova
> replica o shape dele, mas é um tipo separado em `Adapters/Models`.

---

## Passos ordenados
1. Criar a ViewModel (shape = `OrdemServicoResumoDto`) + `OrdemServicoPresenter` (migrar a projeção do `RegistrarDiagnosticoHandler` e equivalentes de command).
2. Alterar os 8 handlers de comando para `Result<OrdemServico>`; remover montagem de DTO. **Build**.
3. Criar `OrdemServicoController` (CA) com os 8 métodos de command.
4. Mover `RegistrarDiagnosticoRequest` para Adapters/Models.
5. Renomear o MVC para `OrdemServicoApiController`; tornar finos **só os 8 endpoints de command** —
   status idêntico ao atual (`Gerar`→`CreatedAtAction`/`UnprocessableEntity`; PATCH→`Ok`/`UnprocessableEntity`).
   Os 2 endpoints de query ficam como estão (`Send` direto).
6. DI no `OrdemServicoModule.cs`. **Build solução + testes.**

## Testes
- `OrdemServico.Application.Tests` — asserts dos 8 handlers de comando agora sobre `OrdemServico`. Migrar verificação de shape para teste do Presenter (recomendado) ou ajustar.
- Integração — JSON dos 8 endpoints de command idêntico.

## Definition of Done
- [ ] `OrdemServicoController` (CA, 8 métodos) + `OrdemServicoPresenter` + ViewModel criados.
- [ ] 8 handlers de comando devolvem `OrdemServico`; montagem inline removida.
- [ ] `OrdemServicoApiController`: 8 endpoints finos; 2 queries ainda no fluxo antigo (esperado).
- [ ] `OrdemServicoResumoDto` preservado no caminho cross-module.
- [ ] Build + testes verdes; contrato HTTP idêntico.
