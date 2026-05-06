using Cadastro.Application.Veiculos.Commands.CadastrarVeiculo;
using Cadastro.Application.Veiculos.Queries.ListarVeiculos;
using Cadastro.Application.Veiculos.Queries.ListarVeiculosPorCliente;
using Cadastro.Application.Veiculos.Queries.ObterVeiculoPorId;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cadastro.Presentation.Controllers;


[ApiController]
[Route("api/v1")]
public class VeiculosController : ControllerBase
{
    private readonly ISender _sender;

    public VeiculosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("veiculos")]
    public async Task<IActionResult> Criar([FromBody] CadastrarVeiculoCommand command)
    {
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return CreatedAtAction(nameof(ObterPorId), new { id = result.Value.VeiculoId }, result.Value);
    }

    [HttpGet("veiculos")]
    public async Task<IActionResult> Listar()
    {
        var result = await _sender.Send(new ListarVeiculosQuery());

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("veiculos/{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var result = await _sender.Send(new ObterVeiculoPorIdQuery(id));

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("clientes/{id}/veiculos")]
    public async Task<IActionResult> ObterVeiculosPorClienteId(Guid id)
    {
        var result = await _sender.Send(new ListarVeiculosPorClienteQuery(id));

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

}


