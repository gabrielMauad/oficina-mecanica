using Cadastro.Application.Clientes.Commands.AtualizarCliente;
using Cadastro.Application.Clientes.Commands.CadastrarCliente;
using Cadastro.Application.Clientes.Commands.DesativarCliente;
using Cadastro.Application.Clientes.Queries.ListarClientes;
using Cadastro.Application.Clientes.Queries.ObterClientePorId;
using Cadastro.Presentation.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cadastro.Presentation.Controllers;

[ApiController]
[Route("api/v1/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ISender _sender;

    public ClientesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CadastrarClienteCommand command)
    {
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return CreatedAtAction(nameof(ObterPorId), new { id = result.Value.ClienteId }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var result = await _sender.Send(new ListarClientesQuery());

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var result = await _sender.Send(new ObterClientePorIdQuery(id));

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/nome")]
    public async Task<IActionResult> AtualizarNome(Guid id, [FromBody] AtualizarNomeRequest request)
    {
        var result = await _sender.Send(new AtualizarClienteCommand(id, request.Nome, null));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/telefone")]
    public async Task<IActionResult> AtualizarTelefone(Guid id, [FromBody] AtualizarTelefoneRequest request)
    {
        var result = await _sender.Send(new AtualizarClienteCommand(id, null, request.Telefone));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var result = await _sender.Send(new DesativarClienteCommand(id));

        if (result.IsFailure)
            return NotFound(result.Error);

        return NoContent();
    }
}
