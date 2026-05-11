namespace OrdemServico.Contracts.Dtos;

public sealed record OrcamentoDto(
    decimal ValorTotal,
    string Status,
    DateTime DataGeracao,
    DateTime? DataEnvio,
    DateTime? DataAprovacao
);
