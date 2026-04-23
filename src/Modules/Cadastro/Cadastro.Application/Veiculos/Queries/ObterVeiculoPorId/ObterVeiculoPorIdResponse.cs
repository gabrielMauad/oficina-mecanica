using Cadastro.Domain.Veiculo;

namespace Cadastro.Application.Veiculos.Queries.ObterVeiculoPorId;

public sealed record ObterVeiculoPorIdResponse(
    Guid Id,
    string Placa,
    string Modelo,
    string Marca,
    int Ano,
    Guid ClienteId,
    DateTime CadastradoEm,
    DateTime AtualizadoEm
)
{
    public static ObterVeiculoPorIdResponse FromVeiculo(Veiculo veiculo)
    {
        return new ObterVeiculoPorIdResponse(
            Id: veiculo.Id.Value,
            Placa: veiculo.Placa.Numero,
            Modelo: veiculo.Modelo,
            Marca: veiculo.Marca,
            Ano: veiculo.Ano,
            ClienteId: veiculo.ClienteId.Value,
            CadastradoEm: veiculo.CadastradoEm,
            AtualizadoEm: veiculo.AtualizadoEm
        );
    }
}

