using MediatR;
using OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;
using SharedKernel.Domain;

namespace OrdensServico.Application.Ordens.Queries.ListarOrdensParaAcompanhamento;

public sealed class ListarOrdensParaAcompanhamentoHandler
    : IRequestHandler<ListarOrdensParaAcompanhamentoQuery, Result<List<OrdemServicoListItem>>>
{
    private readonly IListarOrdensParaAcompanhamentoReadModel _readModel;

    public ListarOrdensParaAcompanhamentoHandler(IListarOrdensParaAcompanhamentoReadModel readModel) =>
        _readModel = readModel;

    public async Task<Result<List<OrdemServicoListItem>>> Handle(
        ListarOrdensParaAcompanhamentoQuery request,
        CancellationToken ct
    )
    {
        return await _readModel.Listar(ct);
    }
}
