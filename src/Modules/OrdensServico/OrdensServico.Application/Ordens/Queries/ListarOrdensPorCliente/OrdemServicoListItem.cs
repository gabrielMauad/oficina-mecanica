namespace OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;

public sealed record OrdemServicoListItem(
    Guid Id,
    Guid ClienteId,
    Guid VeiculoId,
    string Status,
    string? DescricaoDiagnostico,
    DateTime? NotificadoEm,
    DateTime? EntregueEm,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    IReadOnlyList<ItemServicoListItem> ItensServico,
    IReadOnlyList<ItemPecaListItem> ItensPeca,
    IReadOnlyList<OrcamentoListItem> Orcamentos
);

public sealed record ItemServicoListItem(
    Guid ServicoId,
    int Quantidade,
    decimal PrecoUnitarioSnapshot
);

public sealed record ItemPecaListItem(
    Guid PecaInsumoId,
    int Quantidade,
    decimal PrecoUnitarioSnapshot
);

public sealed record OrcamentoListItem(
    decimal ValorTotal,
    string Status,
    DateTime DataGeracao,
    DateTime? DataEnvio,
    DateTime? DataAprovacao
);
