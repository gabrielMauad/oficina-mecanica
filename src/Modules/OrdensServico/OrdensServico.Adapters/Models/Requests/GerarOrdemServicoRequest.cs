namespace OrdensServico.Adapters.Models.Request;

public sealed record GerarOrdemServicoRequest(Guid ClienteId, Guid VeiculoId);
