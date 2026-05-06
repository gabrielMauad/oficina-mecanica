
namespace Cadastro.Domain.Servico;
public interface IServicoRepository
{
    Task Adicionar(Servico servico, CancellationToken ct = default);
    Task<Servico?> ObterPorId(ServicoId id, CancellationToken ct = default);
    Task<bool> ExistePorNome(string nome, CancellationToken ct = default);
    Task Atualizar(Servico servico, CancellationToken ct = default);
}

