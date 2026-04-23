using Cadastro.Domain.Veiculo;

namespace Cadastro.Application.Veiculos.Commands.CadastrarVeiculo;

public sealed record CadastrarVeiculoResponse(
    Guid VeiculoId,
    string Placa,
    string Modelo,
    string Marca,
    int Ano,
    Guid ClienteId,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static CadastrarVeiculoResponse FromVeiculo(Veiculo veiculo)
    {
        return new CadastrarVeiculoResponse(
            veiculo.Id.Value,
            veiculo.Placa.Numero,
            veiculo.Modelo,
            veiculo.Marca,
            veiculo.Ano,
            veiculo.ClienteId.Value,
            veiculo.CadastradoEm,
            veiculo.AtualizadoEm
        );
    }
}

