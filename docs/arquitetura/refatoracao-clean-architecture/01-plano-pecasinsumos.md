# Plano 01 — Fundação + PecasInsumos (piloto)

> Ler `00-referencia.md` antes. Este plano **estabelece o template concreto** de toda a refatoração
> aplicando as convenções end-to-end no módulo **PecasInsumos** (médio, autocontido, sem ACL consumindo
> outros módulos). O resultado vira a referência de implementação para os módulos seguintes.

**Pré-condição:** nenhuma.
**Estado final:** PecasInsumos 100% no novo desenho; build + testes verdes; contrato HTTP idêntico.

---

## Escopo

Módulo `PecasInsumos` — 1 agregado (`PecaInsumo`), 5 commands + 2 queries, 1 controller, 1 repositório.

**Operações:** `AdicionarPecaInsumo`, `AtualizarPecaInsumo`, `IncrementarEstoque`, `DecrementarEstoque`,
`DesativarPecaInsumo` (commands); `ListarPecasInsumos`, `ObterPecaInsumoPorId` (queries).

### Fora de escopo (tratado em outro lugar / não tocar)
- `IntegrationEventHandlers/` (`DecrementarEstoqueQuandoOrcamentoGerado`, `IncrementarEstoqueQuandoOrcamentoRejeitado`) — **permanecem em Application, inalterados**. ⚠️ Ver passo de verificação 7.
- `Contracts` (`IPecaInsumoQuery`, `IPecasInsumosDisponibilidadeQuery`, DTOs) — **inalterado**.
- Migrations / EF mappings — inalterados.

---

## Arquivos

### Criar
- `PecasInsumos.Adapters/PecasInsumos.Adapters.csproj` (refs: `PecasInsumos.Application`, `PecasInsumos.Domain`, MediatR; **sem** ASP.NET/EF).
- `PecasInsumos.Adapters/Controllers/PecaInsumoController.cs` — CA Controller (POCO, `ISender` + Presenter), 7 métodos.
- `PecasInsumos.Adapters/Presenters/PecaInsumoPresenter.cs` — `Present(PecaInsumo)` → ViewModel; `Present` para listas/leitura.
- `PecasInsumos.Adapters/Gateways/PecaInsumoGateway.cs` (`: IPecaInsumoGateway`) — delega a `IPecaInsumoRepository`.
- `PecasInsumos.Adapters/Models/` — Request Models (record sem atributos) + ViewModels (mesmo shape dos atuais `*Response`).
- `PecasInsumos.Application/Gateways/IPecaInsumoGateway.cs` — port do use case (assinaturas espelhando o repo: `ObterPorId`, `Adicionar`, `Atualizar`...).

### Mover / renomear
- `PecasInsumos.Domain/IPecaInsumoRepository.cs` → `PecasInsumos.Adapters/DataSources/IPecaInsumoRepository.cs` (anel verde — cliente é o Gateway, não o use case; ver `00-referencia §3.2`). Atualizar namespace + todos os usings.
- `PecasInsumos.Presentation/` → **renomear projeto para `PecasInsumos.Web`** (csproj, pasta, namespace raiz, `PecasInsumosAssemblyMarker`).
- `PecasInsumos.Presentation/Controllers/PecasInsumosController.cs` → `PecasInsumos.Web/Controllers/PecaInsumoApiController.cs` (MVC fino).
- `PecasInsumos.Presentation/Models/*` → mover os Request HTTP para `PecasInsumos.Adapters/Models/` (ou recriar como records framework-free).

### Modificar
- **Handlers (7):** trocar injeção `IPecaInsumoRepository` → `IPecaInsumoGateway`; commands passam a retornar `Result<PecaInsumo>` (entidade) em vez do `*Response`; remover montagem inline de `*Response`.
- **`*Response` (em Application):** os campos viram a ViewModel correspondente em `Adapters/Models`. O método `FromX` vira o `Present` do Presenter. Deletar os `*Response` antigos após migrar (ver §código morto).
- `PecasInsumos.Infrastructure/PecaInsumoRepository.cs` — só atualizar o `using` de `IPecaInsumoRepository` (agora em `PecasInsumos.Adapters.DataSources`; o Infra já referencia `Adapters`).
- `PecasInsumos.Infrastructure/PecasInsumosModule.cs` — referenciar `PecasInsumos.Adapters`; registrar `IPecaInsumoGateway`→`PecaInsumoGateway`, `PecaInsumoController` (CA), `PecaInsumoPresenter`.
- `src/Bootstrap/Api/Program.cs` — `AddApplicationPart(typeof(PecasInsumosAssemblyMarker).Assembly)` agora aponta para o assembly `PecasInsumos.Web`.
- Solução `.slnx` / referências de projeto: adicionar `PecasInsumos.Adapters`, renomear `Presentation`→`Web`.

### Deletar (código morto, ao final)
- `*Response` antigos com `FromX` (substituídos por ViewModel + Presenter), se não mais referenciados.

---

## Passos ordenados

1. Criar `PecasInsumos.Adapters.csproj` e adicioná-lo à solução; ajustar refs.
2. Mover `IPecaInsumoRepository` para `Adapters/DataSources/`; criar `Application/Gateways/IPecaInsumoGateway.cs`.
3. Criar `PecaInsumoGateway` (Adapters) delegando ao repo.
4. Religar os 7 handlers ao `IPecaInsumoGateway`. **Build** (ainda retornando `*Response`). Rodar testes — verde esperado.
5. Criar ViewModels + Presenter (migrar lógica dos `FromX`). Alterar handlers de comando para retornar `Result<PecaInsumo>`. **Queries:** `ObterPorId` carrega o agregado via gateway → devolve `Result<PecaInsumo>` (entidade) e o Presenter monta a ViewModel; `Listar` é projeção pura (query object) → devolve o read model e o Presenter mapeia. **Não deixar `FromX`/`*Response` dentro de nenhum handler** (ver `00-referencia §3.3`).
6. Criar `PecaInsumoController` (CA): monta Command, `Send`, chama Presenter, retorna `Result<ViewModel>`.
7. ⚠️ **Verificar IntegrationEventHandlers:** confirmar que `DecrementarEstoqueQuandoOrcamentoGerado`/`IncrementarEstoqueQuandoOrcamentoRejeitado` (que disparam os commands `Decrementar/Incrementar`) **ainda compilam** após a mudança de retorno dos handlers (devem ignorar o `Result.Value` ou usar só `IsFailure`). Ajustar uso se necessário, sem mudar comportamento.
8. Renomear `Presentation`→`Web`; reescrever o MVC como `PecaInsumoApiController` fino (injeta `PecaInsumoController` CA; mapeia `Result`→status **por endpoint**, idêntico ao atual).
9. Atualizar `PecasInsumosModule.cs` (DI) e `Program.cs` (`AddApplicationPart`).
10. **Build solução + testes** (unit + integração). Remover código morto.

---

## Testes
- `tests/Modules/PecasInsumos/PecasInsumos.Application.Tests` — os testes mockam o `IPecaInsumoGateway` (não o repositório), então o move do `IPecaInsumoRepository` para Adapters não os afeta. Ajustar asserts que esperavam `*Response`: o handler de `ObterPorId` (e os commands) agora devolve a entidade `PecaInsumo` — assertar sobre a entidade (ex.: `result.Value.PrecoUnitario.Valor`, `result.Value.UnidadeDeMedida` como enum). Onde os testes validavam o shape HTTP de saída, mover essa asserção para um teste do Presenter (opcional).
- `tests/Modules/PecasInsumos/PecasInsumos.Domain.Tests` — não deve mudar.
- `tests/IntegrationTests` — endpoints de peças devem responder JSON idêntico. Verde sem alteração; se algum shape divergir, corrigir a ViewModel (não o teste).

## Definition of Done
- [ ] `PecasInsumos.Adapters` criado; `Web` renomeado; build verde.
- [ ] Fluxo `ApiController → Controller CA → Send → Handler → Gateway → Repo`; saída via Presenter.
- [ ] `IPecaInsumoGateway` em Application (`Gateways/`); `IPecaInsumoRepository`(DataSource) em Adapters (`DataSources/`); impls nos anéis certos.
- [ ] IntegrationEventHandlers intactos e compilando.
- [ ] Todos os testes verdes; contrato HTTP idêntico; código morto removido.
- [ ] `Adapters` sem ASP.NET/EF (conferir csproj).
