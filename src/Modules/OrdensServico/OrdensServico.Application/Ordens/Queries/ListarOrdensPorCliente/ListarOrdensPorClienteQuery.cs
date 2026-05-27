using MediatR;
using OrdensServico.Contracts.Dtos;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;

public sealed record ListarOrdensPorClienteQuery(Guid ClienteId) : IRequest<Result<List<OrdemServicoResumoDto>>>;
