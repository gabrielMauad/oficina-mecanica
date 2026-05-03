using Cadastro.Application.Veiculos.Queries.ListarVeiculosPorCliente;
using Cadastro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cadastro.Infrastructure.Queries;

internal sealed class ListarVeiculosPorClienteQueryImpl : IListarVeiculosPorClienteQuery
{
    private readonly CadastroDbContext _context;

    public ListarVeiculosPorClienteQueryImpl(CadastroDbContext context) => _context = context;

    public Task<VeiculosPorCliente?> ListarPorClienteId(Guid clienteId, CancellationToken ct = default) =>
        _context.Clientes
            .AsNoTracking()
            .Where(c => c.Id.Value == clienteId)
            .Select(c => new VeiculosPorCliente(
                c.Id.Value,
                c.Nome,
                _context.Veiculos
                    .Where(v => v.ClienteId.Value == clienteId)
                    .Select(v => new VeiculoDoCliente(
                        v.Id.Value,
                        v.Placa.Numero,
                        v.Modelo,
                        v.Marca,
                        v.Ano,
                        v.CadastradoEm,
                        v.AtualizadoEm))
                    .ToList()))
            .FirstOrDefaultAsync(ct);
}
