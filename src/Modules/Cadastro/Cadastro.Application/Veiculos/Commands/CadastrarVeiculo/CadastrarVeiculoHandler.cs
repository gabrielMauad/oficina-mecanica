using Cadastro.Domain.Cliente;
using Cadastro.Domain.Veiculo;
using MediatR;
using SharedKernel.Domain;

namespace Cadastro.Application.Veiculos.Commands.CadastrarVeiculo;

public sealed class CadastrarVeiculoHandler
    : IRequestHandler<CadastrarVeiculoCommand, Result<CadastrarVeiculoResponse>>
{
    private readonly IVeiculoRepository _repository;
    private readonly IClienteRepository _clienteRepository;

    public CadastrarVeiculoHandler(
        IVeiculoRepository repository,
        IClienteRepository clienteRepository)
    {
        _repository = repository;
        _clienteRepository = clienteRepository;
    }

    public async Task<Result<CadastrarVeiculoResponse>> Handle(CadastrarVeiculoCommand command, CancellationToken cancellationToken)
    {
        var placaNormalizada = command.Placa.ToUpperInvariant().Replace("-", "");

        if (await _repository.ExistePorPlaca(placaNormalizada, cancellationToken))
            return VeiculoErrors.PlacaJaExiste;

        ClienteId clienteId = new(command.ClienteId);
        Cliente? cliente = await _clienteRepository.ObterPorId(clienteId, cancellationToken);

        if (cliente is null)
            return VeiculoErrors.ClienteNaoEncontrado;

        if (!cliente.Ativo)
            return VeiculoErrors.ClienteInativo;

        Result<Veiculo> veiculoResult = Veiculo.Criar(command.Placa, command.Modelo, command.Marca, command.Ano, clienteId);

        if (veiculoResult.IsFailure)
            return veiculoResult.Error;

        Veiculo veiculo = veiculoResult.Value;

        await _repository.Adicionar(veiculo, cancellationToken);

        return CadastrarVeiculoResponse.FromVeiculo(veiculoResult.Value);
    }
}
