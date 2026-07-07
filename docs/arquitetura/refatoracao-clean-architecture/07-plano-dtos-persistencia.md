# Plano 07 — DTOs de Persistência (desacoplar o Domínio do EF Core)

> **Leia `00-referencia.md` primeiro.** Este plano executa o item explicitamente marcado como
> **fora de escopo** na §6 daquele documento ("DTO de persistência — só se sobrar tempo, nascerá dentro
> do Gateway de persistência"). A refatoração Clean Architecture (planos 01–06) está **concluída**; este
> é um trabalho posterior e independente.

**Data:** 2026-07-06
**Status:** **implementado e concluído (2026-07-07)** — build da solução verde, unit + integração (43 testes)
verdes. Ver §8 para o checklist final e a nota de implementação sobre o furo adicional encontrado na fase D.
**Executor previsto:** sessão Sonnet usando este arquivo como spec. Implementar exatamente como descrito.

---

## 1. Objetivo e motivação

Hoje o EF Core mapeia **os próprios agregados/entidades de domínio** diretamente nas tabelas. Isso força o
domínio a satisfazer regras do framework:

- **Construtores privados com nomes de parâmetro casando com propriedades** (o EF materializa por eles).
- **Setters privados em todas as propriedades** (o EF escreve por eles via reflexão).
- **Campos de retaguarda expostos ao mapeamento** — `_itensPeca`, `_itensServico`, `_orcamentos` em
  `OrdemServico`, ligados via `.HasField(...)` / `.Navigation(...)` em `OrdemServicoConfiguration`.
- **Reflexão sobre construtores privados de VOs** dentro das `*Configuration` (`_cpfCtor`, `_cnpjCtor`,
  `_placaCtor`, `_dinheiroCtor`) para reconstruir Value Objects.
- **A coleta de domain events depende do `ChangeTracker` do EF** (ver §4.3) — ou seja, o mecanismo de
  eventos de domínio só funciona porque o agregado está *rastreado pelo ORM*.

**Meta:** o domínio (`{Module}.Domain`) deixa de ser conhecido pelo EF. O EF passa a mapear **DTOs de
persistência** ("Records") — POCOs planos de tipos primitivos que vivem no anel verde. O **Gateway de
persistência** ganha função real: traduz `Domínio ⇄ Record`. O DataSource (Repository) fala apenas Records.

> **Nota sobre os campos `_itensPeca` etc.:** eles **permanecem** no domínio — coleção encapsulada com
> exposição `IReadOnlyList` é bom design DDD, independente de ORM. O que sai é o **acoplamento**: o EF
> deixa de bindar neles. Reconstituição passa a preenchê-los explicitamente (§4.2), não por reflexão do EF.

**Anti-objetivo:** nenhuma mudança de comportamento, contrato HTTP, contrato cross-module (`Contracts`),
schema de banco ou migrations. Build e todos os testes (unit + integração) permanecem verdes.

---

## 2. Os três furos que este plano fecha (leia antes de codar)

Uma implementação ingênua ("crio o Record e mapeio no gateway") **quebra silenciosamente** três coisas.
Cada uma tem seção dedicada:

1. **Domain events param de ser publicados.** São coletados de `ChangeTracker.Entries<IHasDomainEvents>()`
   nos 3 `DbContext`. Com o EF rastreando **Records** (que não são `IHasDomainEvents`), a coleta retorna
   vazio → nenhum e-mail, nenhuma baixa de estoque, nenhum integration event. **Solução: §4.3
   (`IDomainEventCollector`).** Esta é a parte mais crítica.

2. **`Atualizar` de agregado com filhos corrompe os dados.** Hoje `_context.Update(agregado)` funciona
   porque o EF rastreia o grafo carregado (incluindo a troca de filhos em `RegistrarDiagnostico`, que dá
   `Clear()` e recria itens). Com Records destacados, um `Update` cego deixa filhos órfãos e duplica
   linhas. **Solução: §4.4 (reconciliação no Repository).**

3. **Dois domain event handlers persistem via tracking implícito.** `EnviarOrcamentoAoCliente` e
   `NotificarClienteAoFinalizar` fazem `ObterPorId → muta → SaveChangesAsync` **sem** chamar `Atualizar`.
   Hoje funciona porque `ObterPorId` devolve entidade rastreada. Com Records, a entidade vem destacada e a
   mutação se perde. **Solução: §4.5 (adicionar `Atualizar`).** Todos os *command* handlers já chamam
   `Atualizar` — só estes dois não.

---

## 3. Decisões de design (com o porquê)

### 3.1 — Onde vivem os Records e como se chamam

- **Projeto:** `{Module}.Adapters` (anel verde). Novo diretório `DataSources/Records/`.
- **Por que no verde, e não em Infrastructure:** o Record precisa ser visto por **dois** lados — o Gateway
  (verde) que o mapeia, e o EF (azul) que o persiste. O verde **não referencia** o azul, então o Record
  não pode morar no azul. Morando no verde, o `Infrastructure → Adapters` (já existente) dá acesso ao EF.
  É a mesma lógica que já colocou `I{Entity}Repository` no verde (§3.2 da referência).
- **"Mas persistência não é um detalhe azul?"** O *mapeamento* (as `*Configuration`, o `DbContext`, os
  Repositories) continua **100% no azul**. O Record é só um POCO de dados — sem atributos EF, sem `using`
  de framework. O verde continua provado livre de EF pelo compilador. Espelha o `PessoaDto`/`IDataSource`
  do projeto de referência do professor, que ficam no boundary `Comm` visível aos dois lados.
- **Nome:** sufixo **`Record`** (`ClienteRecord`, `OrdemServicoRecord`, ...). **Não** usar `Dto` — colide
  semanticamente com os DTOs de `Contracts` (`ClienteDto`) e confunde na leitura.
- **Forma:** `sealed class` mutável, **só primitivos/BCL** (`Guid`, `string`, `decimal`, `int`, `bool`,
  `DateTime`, `DateTime?`) e `List<{Filho}Record>` para coleções. Setters públicos (o EF e a reconciliação
  precisam escrever). Sem construtor customizado (EF usa o default; inicializar listas com `= []`).

### 3.2 — Fronteiras e responsabilidades (quem fala o quê)

| Peça | Projeto | Antes falava | Depois fala |
|---|---|---|---|
| `I{X}Gateway` | `.Application` 🔴 | Domínio | **Domínio** (inalterado — o use case consome domínio) |
| `{X}Gateway` (impl) | `.Adapters` 🟢 | delega domínio 1:1 | **mapeia Domínio ⇄ Record**; delega Record |
| `I{Entity}Repository` (DataSource) | `.Adapters` 🟢 | Domínio | **Record + `Guid`** (sem `using` de Domínio) |
| `{Entity}Repository` (impl EF) | `.Infrastructure` 🔵 | Domínio via `DbSet<Domínio>` | **Record via `DbSet<Record>`** |
| `*Configuration` | `.Infrastructure` 🔵 | mapeia Domínio (reflexão p/ VOs) | **mapeia Record** (colunas planas, zero reflexão) |
| Query objects (leitura) | `.Infrastructure` 🔵 | projeta de `DbSet<Domínio>` | **projeta de `DbSet<Record>`** (§4.6) |

> **`I{Entity}Gateway` NÃO muda de assinatura.** O use case continua recebendo/entregando agregados de
> domínio. A tradução é confinada entre o Gateway e o DataSource. Este é o "sentido real" do Gateway.

### 3.3 — Reconstituição do domínio (a peça nova no Domínio)

Ao ler, o Gateway precisa **reconstruir** o agregado a partir do Record, no estado exato persistido — com
`Id` existente, `Status`/timestamps do banco, e **sem** disparar eventos nem revalidar. As fábricas
públicas (`Criar`) não servem: geram `Id` novo, setam `UtcNow`, retornam `Result` e recusam entradas.

- **Decisão:** adicionar em cada entidade/VO um método estático **`Reconstituir(...)`** — POCO puro, sem
  framework, que recebe todos os campos e chama um construtor privado que os atribui diretamente (sem
  `UtcNow`, sem `AddDomainEvent`, sem validação). É o padrão DDD de *reconstitution from persistence*.
- **Por que não reflexão no Gateway:** replicaria dentro do Gateway exatamente o hack de reflexão que
  estamos removendo do EF. `Reconstituir` é explícito, type-safe e livre de framework — reforça a narrativa
  "domínio sem ORM" em vez de escondê-la.
- **Visibilidade:** `public` (o Gateway está em outro assembly). Nos filhos do agregado OS
  (`ItemPeca`/`ItemServico`/`Orcamento`) isso abre um pouco a fronteira do agregado; é aceitável para um
  método de reconstituição claramente nomeado. *(Alternativa mais estrita — reconstituição só via raiz com
  records de estado de domínio — descartada por excesso de maquinário para o ganho.)*
- **Regra de Cpf/Cnpj:** hoje o `ClienteConfiguration` decide Cpf vs Cnpj **por comprimento** (`<= 11`).
  Essa regra é de domínio e **muda de lugar** para `Documento.Reconstituir(string numero)`.

### 3.4 — Persistência de agregado com filhos: reconciliação

`OrdemServico.Atualizar` não pode ser um `Update` cego. O Repository carrega o grafo **rastreado**, copia
escalares com `SetValues`, e **reconcilia cada coleção de filhos** por `Id` (remove ausentes, adiciona
novos, atualiza casados). Detalhe e código em §4.4. Agregados de tabela única (Cliente/Servico/Veiculo/
PecaInsumo) usam `Update` simples (§4.7).

### 3.5 — Leitura sempre `AsNoTracking`

Como o Gateway sempre mapeia para domínio (nunca devolve o Record rastreado ao use case), **todo
`ObterPorId` do DataSource usa `AsNoTracking()`**. Isso evita conflito de identidade quando o `Atualizar`
recarrega o grafo rastreado para reconciliar. As queries de leitura já usam `AsNoTracking`.

---

## 4. Especificação técnica transversal

### 4.0 Grafo de dependências (inalterado — nenhuma referência nova de projeto)

```
Web 🔵 ─► Adapters 🟢 ─► Application 🔴 ─► Domain 🟡
Infrastructure 🔵 ─► Adapters 🟢 (já existe: enxerga Records + I{Entity}Repository)
Adapters NUNCA referencia Infrastructure. Records são POCO, não quebram "verde sem EF".
```

### 4.1 Forma dos Records (exaustivo)

Nomes de campo **espelham as propriedades de domínio** (facilita o mapeamento 1:1). Colunas via
`*Configuration` (§4.8).

```csharp
// Cadastro.Adapters/DataSources/Records/ClienteRecord.cs
public sealed class ClienteRecord
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Documento { get; set; } = "";   // dígitos; Cpf/Cnpj decidido no domínio
    public string Email { get; set; } = "";
    public string Telefone { get; set; } = "";
    public bool Ativo { get; set; }
    public DateTime CadastradoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}

// ServicoRecord: Id, Nome, Descricao(string?), PrecoBase(decimal), Ativo, CadastradoEm, AtualizadoEm
// VeiculoRecord: Id, Placa(string), Modelo, Marca, Ano(int), ClienteId(Guid), CadastradoEm, AtualizadoEm
// PecaInsumoRecord: Id, Nome, Descricao(string?), PrecoUnitario(decimal), QuantidadeEmEstoque(int),
//                   UnidadeDeMedida(string), Ativo, CadastradoEm, AtualizadoEm
```

```csharp
// OrdensServico.Adapters/DataSources/Records/OrdemServicoRecord.cs
public sealed class OrdemServicoRecord
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public Guid VeiculoId { get; set; }
    public string Status { get; set; } = "";           // enum como string
    public string? DescricaoDiagnostico { get; set; }
    public DateTime? NotificadoEm { get; set; }
    public DateTime? EntregueEm { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public List<ItemServicoRecord> ItensServico { get; set; } = [];
    public List<ItemPecaRecord> ItensPeca { get; set; } = [];
    public List<OrcamentoRecord> Orcamentos { get; set; } = [];
}
// ItemServicoRecord: Id(Guid), ServicoId(Guid), Quantidade(int), PrecoUnitarioSnapshot(decimal)
// ItemPecaRecord:    Id(Guid), PecaInsumoId(Guid), Quantidade(int), PrecoUnitarioSnapshot(decimal)
// OrcamentoRecord:   Id(Guid), ValorTotal(decimal), Status(string), DataGeracao(DateTime),
//                    DataEnvio(DateTime?), DataAprovacao(DateTime?)
```

> O FK `ordem_servico_id` dos filhos continua **shadow property** (como hoje) — não vira campo do Record.

### 4.2 Métodos `Reconstituir` a adicionar no Domínio

Padrão (exemplo `Cliente`): novo construtor privado "cheio" + fábrica estática. **Não** remover os
construtores/fábricas existentes.

```csharp
// Cliente.cs — adicionar
private Cliente(ClienteId id, string nome, Documento documento, string email, string telefone,
                bool ativo, DateTime cadastradoEm, DateTime atualizadoEm) : base(id)
{
    Nome = nome; Documento = documento; Email = email; Telefone = telefone;
    Ativo = ativo; CadastradoEm = cadastradoEm; AtualizadoEm = atualizadoEm;
}

public static Cliente Reconstituir(ClienteId id, string nome, Documento documento, string email,
    string telefone, bool ativo, DateTime cadastradoEm, DateTime atualizadoEm) =>
    new(id, nome, documento, email, telefone, ativo, cadastradoEm, atualizadoEm);
```

VOs (bypass de validação/normalização — o dado do banco já é válido):

```csharp
// Documento.cs (abstrata) — a regra Cpf/Cnpj vem do EF para cá; ÚNICA reconstituição do tipo.
// A base constrói a subclasse direto (ctors internal). NÃO criar Reconstituir em Cpf/Cnpj:
// um static de mesma assinatura no filho ESCONDE o do pai (warning CS0108).
public static Documento Reconstituir(string numero) =>
    numero.Length <= 11 ? new Cpf(numero) : new Cnpj(numero);
// Cpf.cs / Cnpj.cs: trocar o ctor de `private` para `internal` (a base os constrói); sem Reconstituir próprio.
// Placa.cs: public static Placa Reconstituir(string numero) => new(numero);
// Dinheiro.cs (Cadastro E PecasInsumos): public static Dinheiro Reconstituir(decimal v) => new(v);
```

Lista completa de `Reconstituir` a criar:
- **Cadastro.Domain:** `Cliente`, `Servico`, `Veiculo`, `Documento`, `Cpf`, `Cnpj`, `Placa`, `Dinheiro`.
- **PecasInsumos.Domain:** `PecaInsumo`, `Dinheiro`. (`UnidadeDeMedida` é enum → `Enum.Parse` no Gateway.)
- **OrdensServico.Domain:** `OrdemServico`, `ItemServico`, `ItemPeca`, `Orcamento`.

`OrdemServico.Reconstituir` recebe as coleções já como entidades de domínio e as atribui aos campos de
retaguarda:

```csharp
// OrdemServico.cs — adicionar
private OrdemServico(OrdemServicoId id, Guid clienteId, Guid veiculoId, StatusOrdemServico status,
    string? descricaoDiagnostico, DateTime? notificadoEm, DateTime? entregueEm,
    DateTime criadoEm, DateTime atualizadoEm,
    IEnumerable<ItemServico> itensServico, IEnumerable<ItemPeca> itensPeca,
    IEnumerable<Orcamento> orcamentos) : base(id)
{
    ClienteId = clienteId; VeiculoId = veiculoId; Status = status;
    DescricaoDiagnostico = descricaoDiagnostico; NotificadoEm = notificadoEm; EntregueEm = entregueEm;
    CriadoEm = criadoEm; AtualizadoEm = atualizadoEm;
    _itensServico.AddRange(itensServico); _itensPeca.AddRange(itensPeca); _orcamentos.AddRange(orcamentos);
}

public static OrdemServico Reconstituir(/* mesmos parâmetros */) => new(/* ... */);
```

### 4.3 `IDomainEventCollector` — desacoplar eventos do ChangeTracker (FURO #1)

**Novos arquivos em `SharedKernel.Application`:**

```csharp
// IDomainEventCollector.cs
using SharedKernel.Domain;
namespace SharedKernel.Application;

public interface IDomainEventCollector
{
    void Registrar(IHasDomainEvents agregado);
    IReadOnlyList<IDomainEvent> Coletar();
    void Limpar();
}

// DomainEventCollector.cs  (scoped)
public sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly List<IHasDomainEvents> _agregados = [];
    public void Registrar(IHasDomainEvents agregado)
    {
        if (!_agregados.Contains(agregado)) _agregados.Add(agregado); // dedup por Entity.Equals (Id+tipo)
    }
    public IReadOnlyList<IDomainEvent> Coletar() => _agregados.SelectMany(a => a.DomainEvents).ToList();
    public void Limpar()
    {
        foreach (var a in _agregados) a.ClearDomainEvents();
        _agregados.Clear();
    }
}
```

**`SharedKernelModule.AddSharedKernelServices`:** registrar
`services.AddScoped<IDomainEventCollector, DomainEventCollector>();`.
(Confirmar que `AddSharedKernelServices` é chamado uma vez no startup — em `Bootstrap/Api`.)

**`TransactionBehavior`** passa a coletar do collector em vez dos `DbContext`:

```csharp
// injeta IDomainEventCollector _collector (além de _unitOfWorks, _pendingEvents, _publisher)
var response = await next(cancellationToken);
if (response is IResult { IsFailure: true }) return response;

var domainEvents = _collector.Coletar();                 // <== antes: _unitOfWorks.SelectMany(CollectDomainEvents)
foreach (var uow in _unitOfWorks) await uow.SaveChangesAsync(cancellationToken);
_collector.Limpar();                                     // <== antes: uow.ClearDomainEvents()
foreach (var e in domainEvents) await _publisher.Publish(e, cancellationToken);
foreach (var publish in _pendingEvents.GetPending()) await publish(cancellationToken);
return response;
```

**`IUnitOfWork`** encolhe para só persistência:

```csharp
public interface IUnitOfWork { Task<int> SaveChangesAsync(CancellationToken cancellationToken = default); }
```

**Os 3 `DbContext`** (`CadastroDbContext`, `OrdensServicoDbContext`, `PecasInsumosDbContext`) removem
`CollectDomainEvents`/`ClearDomainEvents` e o `using` de `IHasDomainEvents`. Continuam `: DbContext, IUnitOfWork`
(o `SaveChangesAsync` vem do `DbContext`). Os `DbSet` mudam para os Records (§4.8).

**Quem popula o collector:** os **Gateways de persistência** (§4.5) chamam `_collector.Registrar(agregado)`
em `Adicionar`/`Atualizar`. Todos os fluxos que disparam evento passam por um desses (verificado: todos os
command handlers chamam `Adicionar`/`Atualizar`).

### 4.4 Reconciliação do agregado OS no `OrdemServicoRepository` (FURO #2)

```csharp
public async Task Atualizar(OrdemServicoRecord incoming, CancellationToken ct = default)
{
    var tracked = await _context.OrdensServico
        .Include(o => o.ItensServico).Include(o => o.ItensPeca).Include(o => o.Orcamentos)
        .FirstOrDefaultAsync(o => o.Id == incoming.Id, ct);
    if (tracked is null) return;

    _context.Entry(tracked).CurrentValues.SetValues(incoming);          // escalares da raiz
    Sync(_context, tracked.ItensServico, incoming.ItensServico, r => r.Id);
    Sync(_context, tracked.ItensPeca,    incoming.ItensPeca,    r => r.Id);
    Sync(_context, tracked.Orcamentos,   incoming.Orcamentos,   r => r.Id);
}

private static void Sync<T>(DbContext ctx, List<T> atuais, List<T> novos, Func<T, Guid> key) where T : class
{
    foreach (var a in atuais.Where(a => novos.All(n => key(n) != key(a))).ToList())
        ctx.Remove(a);                                                   // filho removido
    foreach (var n in novos)
    {
        var existente = atuais.FirstOrDefault(a => key(a) == key(n));
        if (existente is null) atuais.Add(n);                            // filho novo (EF insere)
        else ctx.Entry(existente).CurrentValues.SetValues(n);           // filho casado (update)
    }
}
```

Cobre os dois padrões reais: `RegistrarDiagnostico` (regenera os `Id` dos filhos → remove todos os antigos
e insere os novos) e transições de status (mesmos `Id` → `SetValues`, ex.: `Orcamento.Status`).
`Adicionar` continua `_context.OrdensServico.Add(record)`. `ObterPorId` usa `AsNoTracking()` + os 3
`Include`.

### 4.5 Gateways de persistência: mapear + registrar no collector

Exemplo `OrdemServicoGateway` (impl final). Mapeamento em `static` mapper co-locado
(`Adapters/DataSources/Mappers/OrdemServicoMapper.cs`) para manter o gateway legível.

```csharp
public sealed class OrdemServicoGateway : IOrdemServicoGateway
{
    private readonly IOrdemServicoRepository _repository;
    private readonly IDomainEventCollector _collector;
    public OrdemServicoGateway(IOrdemServicoRepository repository, IDomainEventCollector collector)
    { _repository = repository; _collector = collector; }

    public async Task Adicionar(OrdemServico os, CancellationToken ct = default)
    { _collector.Registrar(os); await _repository.Adicionar(OrdemServicoMapper.ToRecord(os), ct); }

    public async Task<OrdemServico?> ObterPorId(OrdemServicoId id, CancellationToken ct = default)
    { var r = await _repository.ObterPorId(id.Value, ct); return r is null ? null : OrdemServicoMapper.ToDomain(r); }

    public async Task Atualizar(OrdemServico os, CancellationToken ct = default)
    { _collector.Registrar(os); await _repository.Atualizar(OrdemServicoMapper.ToRecord(os), ct); }
}
```

Mapper (`ToRecord` copia primitivos + `.ToString()` nos enums, `.Value`/`.Numero` nos VOs/ids; `ToDomain`
chama `Reconstituir`). Exemplo dos trechos não triviais:

```csharp
// ToRecord: Status = os.Status.ToString(), ItensPeca = [.. os.ItensPeca.Select(...)], etc.
// ToDomain:
var itensPeca = r.ItensPeca.Select(i => ItemPeca.Reconstituir(new ItemPecaId(i.Id), i.PecaInsumoId,
                                        i.Quantidade, i.PrecoUnitarioSnapshot));
var status = Enum.Parse<StatusOrdemServico>(r.Status);
return OrdemServico.Reconstituir(new OrdemServicoId(r.Id), r.ClienteId, r.VeiculoId, status,
    r.DescricaoDiagnostico, r.NotificadoEm, r.EntregueEm, r.CriadoEm, r.AtualizadoEm,
    itensServico, itensPeca, orcamentos);
```

Os **5 gateways de persistência**: `ClienteGateway`, `ServicoGateway`, `VeiculoGateway` (Cadastro),
`PecaInsumoGateway` (PecasInsumos), `OrdemServicoGateway` (OrdensServico). Todos: injetam
`IDomainEventCollector`, registram em `Adicionar`/`Atualizar`, mapeiam Domínio⇄Record.
Métodos `Existe*` (que recebem/retornam primitivos) **não mudam de assinatura** — só repassam ao DataSource.

> Os gateways de **ACL** de OrdensServico (`ClienteGateway`, `VeiculoGateway`, `ServicoGateway`,
> `PecaDisponibilidadeGateway`, `PecaInsumoInfoGateway`, `NotificacaoClienteGateway`) **não são de
> persistência** (falam via `Contracts`), **não tocam Record nem collector** e ficam **intocados**.

**Fluxo dos 2 domain event handlers (FURO #3):** em `EnviarOrcamentoAoCliente` e
`NotificarClienteAoFinalizar`, inserir `await _ordemServicoGateway.Atualizar(ordemServico, ct);` **antes** de
`await _unitOfWork.SaveChangesAsync(ct);`.

### 4.6 Queries de leitura → projetar do Record

Todas projetam de `DbSet<Domínio>` hoje; passam a projetar de `DbSet<Record>`. Como o Record é plano, a
projeção simplifica: `c.Documento.Numero`→`c.Documento`; `os.Status.ToString()`→`os.Status`;
`oc.Status.ToString()`→`oc.Status`; `pi.PrecoUnitario.Valor`→`pi.PrecoUnitario`; `.Id.Value`→`.Id`.
**Semântica idêntica** (os `Contracts`/read models não mudam).

Arquivos a reescrever (13):
- **Cadastro (7):** `ListarClientesQueryImpl`, `ListarServicosQueryImpl`, `ListarVeiculosQueryImpl`,
  `ListarVeiculosPorClienteQueryImpl`, `CadastroClienteQuery`, `CadastroServicoQuery`, `CadastroVeiculoQuery`.
- **OrdensServico (3):** `ListarOrdensPorClienteQueryImpl`, `ListarOrdensPorClienteReadModelImpl`,
  `OrdemServicoResumoQuery`.
- **PecasInsumos (3):** `ListarPecasInsumosQueryImpl`, `PecaInsumoQuery`, `PecasInsumosDisponibilidadeQuery`.

Nas queries de OS, manter os 3 `Include` (agora sobre as `List<...Record>`). Filtros por id que hoje
constroem VO de id (`new OrdemServicoId(id)`, `new PecaInsumoId(...)`) passam a comparar `Guid` direto
(`o.Id == id`).

### 4.7 / 4.8 Repositories e Configurations

**DataSources** (`I{Entity}Repository` em Adapters e impl em Infrastructure): trocar todo tipo de domínio
por Record; ids de parâmetro viram `Guid`. Remover `using` de Domínio das interfaces (ficam livres de
domínio, como o `IDataSource` de referência). Tabela-única `Atualizar` = `_context.Set<Record>().Update(rec)`
(seguro: reads são `AsNoTracking`). `Existe*` por SQL cru **inalterados**.

**Configurations:** reescritas para `IEntityTypeConfiguration<{Entity}Record>`. Some **toda** reflexão
(`_cpfCtor`, `_placaCtor`, `_dinheiroCtor`, ...) e todo `HasConversion` de VO/id — vira `Property` de
coluna plana. Enum→string: como o Record já guarda `string`, é `Property(x => x.Status)` simples (sem
`HasConversion<string>`). Manter **nomes de coluna, tipos, `HasMaxLength`, `HasPrecision(10,2)`, índices
únicos, defaults, check constraint do documento, e as relações `HasMany().WithOne().HasForeignKey("ordem_servico_id")`**
idênticos (agora as navegações são `List<...Record>` públicas → dispensa `.HasField`/`.Navigation`).
O `HasOne<Cliente>()` de `VeiculoConfiguration` vira `HasOne<ClienteRecord>()`.

**DbContext:** `DbSet<Cliente>`→`DbSet<ClienteRecord>` etc.; `Set<Record>()`. `ApplyConfigurationsFromAssembly`
e `HasDefaultSchema` inalterados.

**DI (`*Module.cs`):** apenas trocar os tipos genéricos das registrações de DataSource/Gateway se
necessário (as interfaces têm o mesmo nome; só o gateway agora depende do collector — resolvido pelo DI
automaticamente). Nenhuma registração nova além do collector (§4.3).

---

## 5. Migrations / snapshot do EF

**Nenhuma migration nova é necessária.** Os Records mapeiam **exatamente** as mesmas tabelas/colunas/tipos/
constraints. O schema não muda → o banco e os testes de integração (que aplicam as migrations existentes no
startup) seguem válidos. O `ModelSnapshot.cs` referencia os tipos CLR antigos, mas o snapshot só é usado em
*design-time* para o diff da **próxima** migration — não afeta runtime nem testes. **Não** rodar
`ef migrations add` (geraria diff vazio ou ruído). Deixar migrations/snapshot como estão.

> Se, ao final, quiser higienizar o snapshot, é opcional e fora do caminho crítico — não fazer sem
> necessidade.

---

## 6. Testes

- **Unit (Application.Tests):** mockam os `I{X}Gateway` (domínio) → **inalterados**.
- **Unit (Domain.Tests):** exercitam `Criar`/regras → **inalterados**. (Opcional: adicionar teste de
  round-trip `Reconstituir` ⇄ mapper, mas não obrigatório.)
- **Integração (IntegrationTests):** exercitam via HTTP e aplicam migrations; não tocam `DbSet<Domínio>`
  diretamente (verificado na `OficinaMecanicaWebApplicationFactory`) → **inalterados**; são a rede de
  segurança que prova comportamento preservado (incluindo o fluxo de eventos: e-mail/estoque).

Critério de aceite = **build da solução verde + todos os testes verdes**, sem alterar asserts.

---

## 7. Ordem de execução (fases com checkpoint verde obrigatório)

Cada fase termina compilando e com testes passando antes de seguir.

| Fase | Escopo | Observações |
|---|---|---|
| **A — Eventos** | `IDomainEventCollector` (§4.3): SharedKernel + `TransactionBehavior` + `IUnitOfWork` + 3 `DbContext` + **os 5 gateways de persistência passam a chamar `Registrar`** (ainda sem Record). | Faz o mecanismo de eventos parar de depender do `ChangeTracker` **antes** de remover o domínio do EF. Pós-fase A a persistência ainda mapeia domínio; tudo verde. É o pré-requisito que fecha o FURO #1 sem janela quebrada. |
| **B — PecasInsumos (piloto)** | `PecaInsumoRecord` + `Reconstituir` + mapper + `PecaInsumoGateway` (mapeia) + `IPecaInsumoRepository`/impl + `PecaInsumoConfiguration` + `DbContext` + 3 queries. | Módulo autocontido (1 tabela, sem filhos). Serve de template. |
| **C — Cadastro** | `ClienteRecord`/`ServicoRecord`/`VeiculoRecord` + `Reconstituir` (inclui VOs `Documento`/`Cpf`/`Cnpj`/`Placa`/`Dinheiro`) + 3 gateways + 3 DataSources + 3 configs + `DbContext` + 7 queries. | Aplica o padrão do piloto ×3. Atenção à regra Cpf/Cnpj em `Documento.Reconstituir`. |
| **D — OrdensServico** | `OrdemServicoRecord` + 3 filhos + `Reconstituir` (raiz + filhos) + mapper + `OrdemServicoGateway` + reconciliação no Repository (§4.4) + 4 configs + `DbContext` + 3 queries + **`Atualizar` nos 2 event handlers (FURO #3)**. | O caso complexo. A reconciliação e o `Atualizar` dos handlers são obrigatórios. |
| **E — Auditoria** | Build solução; todos os testes; grep garantindo que nenhum `*.Domain` é referenciado por `*Configuration`/`DbContext`/Repository/Query; nenhum `using Microsoft.EntityFrameworkCore` em `*.Adapters`; nenhum `ChangeTracker` sobrou. Atualizar `00-referencia.md` §6 (mover item de "fora de escopo" p/ "feito") e docs de arquitetura. | Fechamento. |

Módulos B/C/D são independentes entre si (a fase A os habilita); a ordem é por didática (piloto→simples→
complexo).

---

## 8. Checklist mestre de cobertura

- [x] `IDomainEventCollector` criado, registrado (scoped), e `TransactionBehavior` migrado; `IUnitOfWork`
      enxuto; 3 `DbContext` sem `ChangeTracker`/`IHasDomainEvents`.
- [x] 5 gateways de persistência: injetam collector, `Registrar` em Adicionar/Atualizar, mapeiam Domínio⇄Record.
- [x] Records criados (4 raízes + 3 filhos de OS) em `{Module}.Adapters/DataSources/Records/`, POCO primitivo.
- [x] `Reconstituir` em todas as entidades/VOs listadas (§4.2); regra Cpf/Cnpj movida p/ `Documento`.
- [x] DataSources (interfaces + impls) falam Record + `Guid`; interfaces sem `using` de Domínio.
- [x] `OrdemServicoRepository.Atualizar` reconcilia filhos (§4.4); reads `AsNoTracking`.
- [x] `*Configuration` reescritas p/ Record — zero reflexão/HasConversion de VO; colunas/índices/constraints idênticos.
- [x] `DbSet<...>` dos 3 contextos apontam p/ Records.
- [x] 13 queries de leitura projetam do Record (semântica idêntica).
- [x] 2 domain event handlers chamam `Atualizar` antes de `SaveChangesAsync`.
- [x] Sem migration nova; snapshot inalterado.
- [x] Build da solução verde; unit + integração verdes, sem alterar asserts.
- [x] Auditoria E concluída; docs atualizadas.

> **Nota de implementação (2026-07-07):** a fase D revelou um quarto furo não previsto na spec original:
> ao adicionar um filho novo (`Orcamento`/`ItemPeca`/`ItemServico`) a uma coleção de um agregado já
> rastreado pelo EF, o `DetectChanges` do EF Core **não** infere `EntityState.Added` automaticamente
> quando a chave primária é um `Guid` não-default (gerado no domínio via `Guid.NewGuid()`) — a heurística
> de "chave não é o valor default" leva o EF a assumir que a entidade já existe e gerar `UPDATE` em vez de
> `INSERT`, causando `DbUpdateConcurrencyException` ("0 rows affected"). Corrigido em
> `OrdemServicoRepository.Sync` marcando explicitamente `ctx.Entry(n).State = EntityState.Added` para
> filhos novos. Vale como lição para qualquer reconciliação futura de agregado com filhos de chave
> client-generated.
```