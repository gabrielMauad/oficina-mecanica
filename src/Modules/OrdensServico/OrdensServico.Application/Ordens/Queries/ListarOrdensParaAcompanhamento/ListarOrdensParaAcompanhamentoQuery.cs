using MediatR;
using OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ListarOrdensParaAcompanhamento;

public sealed record ListarOrdensParaAcompanhamentoQuery() : IRequest<Result<List<OrdemServicoListItem>>>;
