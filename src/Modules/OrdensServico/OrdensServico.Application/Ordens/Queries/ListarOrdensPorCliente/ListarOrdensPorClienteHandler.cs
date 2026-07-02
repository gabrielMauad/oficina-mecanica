using MediatR;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;

public sealed class ListarOrdensPorClienteHandler
    : IRequestHandler<ListarOrdensPorClienteQuery, Result<List<OrdemServicoListItem>>>
{
    private readonly IListarOrdensPorClienteReadModel _readModel;

    public ListarOrdensPorClienteHandler(IListarOrdensPorClienteReadModel readModel) =>
        _readModel = readModel;

    public async Task<Result<List<OrdemServicoListItem>>> Handle(
        ListarOrdensPorClienteQuery request,
        CancellationToken ct
    )
    {
        return await _readModel.Listar(request.ClienteId, ct);
    }
}
