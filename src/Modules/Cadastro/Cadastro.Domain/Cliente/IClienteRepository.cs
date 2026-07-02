namespace Cadastro.Domain.Cliente;

public interface IClienteRepository
{
    Task Adicionar(Cliente cliente, CancellationToken ct = default);
    Task<Cliente?> ObterPorId(ClienteId id, CancellationToken ct = default);
    Task<bool> ExistePorDocumento(string documento, CancellationToken ct = default);
    Task Atualizar(Cliente cliente, CancellationToken ct = default);
}
