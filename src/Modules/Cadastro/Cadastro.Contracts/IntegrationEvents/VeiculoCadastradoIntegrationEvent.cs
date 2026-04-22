using SharedKernel.Domain;

namespace Cadastro.Contracts.IntegrationEvents;

public record VeiculoCadastradoIntegrationEvent(Guid EventId, Guid VeiculoId, string Placa, DateTime OcorridoEm) : IIntegrationEvent;
