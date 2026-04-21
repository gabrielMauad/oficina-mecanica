using SharedKernel.Domain;

namespace Cadastro.Domain.Veiculo.Events;
public sealed record VeiculoCadastrado(
    VeiculoId VeiculoId,
    string Marca,
    string Modelo,
    DateTime OcorridoEm
) : IDomainEvent;
