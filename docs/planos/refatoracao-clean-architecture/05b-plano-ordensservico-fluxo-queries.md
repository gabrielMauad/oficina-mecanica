# Plano 05b — OrdensServico: fluxo das queries + fechamento do módulo

> Ler `00-referencia.md`. Conclui o módulo `OrdensServico` levando as **2 queries** ao fluxo verde,
> reutilizando o Controller CA e o Presenter criados no 05a, e remove o último `Send` direto.

**Pré-condição:** Plano 05a concluído (Controller CA, Presenter e ViewModels de command prontos; 8 commands no novo fluxo).
**Estado final:** módulo OrdensServico 100% no novo desenho; build + testes verdes; contrato HTTP idêntico.

---

## Escopo (2 queries)
`ObterOrdemServicoPorId`, `ListarOrdensPorCliente`.

### Fora de escopo
- Tudo dos commands (05a). Gateways/projeto (04). `Contracts` inalterado.

---

## Arquivos

### Criar
- `OrdensServico.Adapters/Models/` — ViewModels de leitura, **se** o shape diferir da ViewModel de command
  (ObterPorId tende a reusar a ViewModel de resumo; ListarPorCliente é coleção). Reusar o que der.

### Modificar
- `OrdensServico.Adapters/Controllers/OrdemServicoController.cs` — **adicionar os 2 métodos de query**
  (montam a Query, `Send`, e passam o read model pelo Presenter/projeção fina).
- `OrdensServico.Adapters/Presenters/OrdemServicoPresenter.cs` — adicionar presentation fina para os read
  models das queries (as queries usam query objects que já projetam; o Presenter apenas adapta para ViewModel).
- `OrdensServico.Web/Controllers/OrdemServicoApiController.cs` — tornar finos os **2 endpoints de query**
  (`ObterPorId`/`ListarPorCliente`→`NotFound` em falha; **manter `[AllowAnonymous]` em `ListarPorCliente`**),
  chamando o Controller CA. Remover o `Send` direto remanescente.
- Handlers de query (2): em geral **inalterados** (já devolvem read model adequado); ajustar só se o
  Presenter exigir um tipo de retorno diferente.

### Deletar (ao final)
- Qualquer montagem inline/`*Response` de OrdensServico que tenha sobrado.

---

## Passos ordenados
1. Adicionar presentation das queries ao `OrdemServicoPresenter`.
2. Adicionar os 2 métodos de query ao `OrdemServicoController` (CA).
3. Tornar finos os 2 endpoints de query no `OrdemServicoApiController`; remover `Send` direto.
4. **Build solução + testes.** Remover código morto.

## Testes
- `OrdemServico.Application.Tests` — queries: asserts em geral mantidos.
- Integração — JSON de `ObterPorId`/`ListarPorCliente` idêntico; `ListarPorCliente` segue anônimo.

## Definition of Done
- [ ] 2 queries no Controller CA; **nenhum** `Send` direto restante no módulo.
- [ ] `[AllowAnonymous]` preservado em `ListarPorCliente`.
- [ ] Build + testes verdes; contrato HTTP idêntico; código morto removido.
- [ ] Módulo OrdensServico inteiro aderente a `00-referencia.md`.
