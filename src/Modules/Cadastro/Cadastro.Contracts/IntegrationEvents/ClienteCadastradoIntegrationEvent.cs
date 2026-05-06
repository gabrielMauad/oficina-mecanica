using SharedKernel.Domain;

namespace Cadastro.Contracts.IntegrationEvents;

public record ClienteCadastradoIntegrationEvent(Guid EventId, Guid ClienteId, string Nome, DateTime OcorridoEm) : IIntegrationEvent;
