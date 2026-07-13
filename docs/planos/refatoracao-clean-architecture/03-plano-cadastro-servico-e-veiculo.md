# Plano 03 — Cadastro: fluxo Servico + Veiculo

> Ler `00-referencia.md`. Reusa a estrutura criada no Plano 02 (`Cadastro.Adapters`, `Cadastro.Web`, DI,
> DataSources já movidos para Adapters, **os 3 Gateways já criados e todos os handlers já usando gateway**).
> Aqui implementamos o **fluxo** completo dos agregados **Servico** e **Veiculo** (Presenter/ViewModel/
> Controller CA/retorno de entidade/slim MVC), finalizando o módulo Cadastro.

**Pré-condição:** Plano 02 concluído (estrutura do Cadastro + Cliente prontos).
**Estado final:** módulo Cadastro 100% no novo desenho; build + testes verdes.

---

## Escopo

- **Servico** — ops: `AdicionarServico`, `AtualizarServico`, `DesativarServico` (commands);
  `ObterServicoPorId`, `ListarServicos` (queries). Controller `ServicosController`. Models:
  `AtualizarDescricaoRequest`, `AtualizarPrecoRequest`.
- **Veiculo** — ops: `CadastrarVeiculo` (command); `ListarVeiculos`, `ListarVeiculosPorCliente`,
  `ObterVeiculoPorId` (queries). Controller `VeiculosController`.

### Fora de escopo
- Estrutura do módulo (já feita no 02): `Contracts` inalterado; DataSources já movidos para Adapters; os
  **3 Gateways já criados** e **todos os handlers já injetam gateway** desde o Plano 02 — aqui **não** se
  cria gateway nem se troca injeção, só se muda o **retorno** dos handlers e se monta o fluxo verde.

---

## Arquivos

### Criar
- `Cadastro.Adapters/Controllers/ServicoController.cs`, `VeiculoController.cs` (CA).
- `Cadastro.Adapters/Presenters/ServicoPresenter.cs`, `VeiculoPresenter.cs`.
- `Cadastro.Adapters/Models/` — Requests + ViewModels de Servico e Veiculo.

> Gateways (`I{Servico,Veiculo}Gateway` + impls) **já existem** desde o Plano 02 — não recriar.

### Mover / renomear
- `Cadastro.Web/Controllers/ServicosController.cs` → `ServicoApiController.cs` (MVC fino).
- `Cadastro.Web/Controllers/VeiculosController.cs` → `VeiculoApiController.cs` (MVC fino).
- Requests HTTP de Servico/Veiculo → `Cadastro.Adapters/Models/`.

### Modificar
- Handlers de **Servico** (5) e **Veiculo** (4): injeção do gateway **já feita no 02**. Aqui: commands retornam `Result<{Servico,Veiculo}>`; queries que carregam o agregado (`ObterPorId`) retornam a entidade e as de projeção pura (`Listar*`) retornam o read model (regra do `00-referencia §3.3`); remover montagem inline/`FromX`.
- `Cadastro.Infrastructure/CadastroModule.cs` — registrar os Controllers CA + Presenters de Servico/Veiculo (os gateways já foram registrados no 02).

### Deletar (ao final)
- `*Response` de Servico/Veiculo migrados para ViewModel + Presenter.

---

## Passos ordenados
1. ViewModels + Presenters; handlers de comando → `Result<Entidade>`; queries pela regra §3.3 (ObterPorId→entidade, Listar→read model); remover `FromX`.
2. `ServicoController`/`VeiculoController` (CA).
3. MVC `ServicoApiController`/`VeiculoApiController` finos — **status por endpoint idêntico ao atual**.
4. DI no `CadastroModule.cs` (Controllers CA + Presenters).
5. **Build solução + testes.** Remover código morto.

## Testes
- `Cadastro.Application.Tests` — mocks de gateway já existem do 02; aqui só ajustar os **asserts** de Servico/Veiculo ao novo retorno (commands e `ObterPorId`→entidade; `Listar*`→read model).
- Integração — endpoints de Servico/Veiculo com JSON idêntico.

## Definition of Done
- [ ] Servico e Veiculo 100% no novo fluxo; gateways em uso.
- [ ] **Nenhum** MVC controller antigo (com `Send` direto) restante no Cadastro.
- [ ] Testes verdes; contrato HTTP idêntico; código morto removido.
- [ ] Módulo Cadastro inteiro aderente ao desenho de `00-referencia.md`.
