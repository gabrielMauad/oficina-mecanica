namespace Cadastro.Domain.Veiculo;

public interface IVeiculoRepository
{
    Task Adicionar(Veiculo veiculo, CancellationToken ct = default);
    Task<Veiculo?> ObterPorId(VeiculoId id, CancellationToken ct = default);
    Task<bool> ExistePorPlaca(string placa, CancellationToken ct = default);
}
