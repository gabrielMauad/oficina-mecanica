using SharedKernel.Domain;

namespace Cadastro.Domain.Servico.Events;

public sealed record ServicoCadastrado(
    ServicoId ServicoId,
    string Nome,
    DateTime OcorridoEm
) : IDomainEvent;

