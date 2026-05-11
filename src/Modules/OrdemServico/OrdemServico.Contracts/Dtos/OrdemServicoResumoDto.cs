namespace OrdemServico.Contracts.Dtos;

public sealed record OrdemServicoResumoDto(
    Guid Id,
    Guid ClienteId,
    Guid VeiculoId,
    string Status,
    string? DescricaoDiagnostico,
    DateTime? NotificadoEm,
    DateTime? EntregueEm,
    DateTime CriadoEm,
    DateTime AtualizadoEm,
    IReadOnlyList<ItemServicoDto> ItensServico,
    IReadOnlyList<ItemPecaDto> ItensPeca,
    IReadOnlyList<OrcamentoDto> Orcamentos
);
