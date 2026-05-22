using MediatR;
using OrdensServico.Contracts.Dtos;
using OrdensServico.Contracts.Queries;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;

public sealed class ListarOrdensPorClienteHandler
    : IRequestHandler<ListarOrdensPorClienteQuery, Result<List<OrdemServicoResumoDto>>>
{
    private readonly IListarOrdensPorClienteQuery _listarOrdensPorClienteQuery;

    public ListarOrdensPorClienteHandler(IListarOrdensPorClienteQuery listarOrdensPorClienteQuery) =>
        _listarOrdensPorClienteQuery = listarOrdensPorClienteQuery;

    public async Task<Result<List<OrdemServicoResumoDto>>> Handle(
        ListarOrdensPorClienteQuery request,
        CancellationToken ct
    )
    {
        var ordens = await _listarOrdensPorClienteQuery.Listar(request.ClienteId, ct);
        return ordens.ToList();
    }
}
