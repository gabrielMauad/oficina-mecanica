using SharedKernel.Domain;

namespace Cadastro.Domain.Cliente.Events;

public sealed record ClienteCadastrado(
    ClienteId ClienteId,
    string Nome,
    DateTime OcorridoEm
) : IDomainEvent;
