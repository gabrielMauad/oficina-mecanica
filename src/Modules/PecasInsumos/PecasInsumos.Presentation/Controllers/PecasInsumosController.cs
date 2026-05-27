using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PecasInsumos.Application.Commands.AdicionarPecaInsumo;
using PecasInsumos.Application.Commands.AtualizarPecaInsumo;
using PecasInsumos.Application.Commands.DecrementarEstoque;
using PecasInsumos.Application.Commands.DesativarPecaInsumo;
using PecasInsumos.Application.Commands.IncrementarEstoque;
using PecasInsumos.Application.Queries.ListarPecasInsumos;
using PecasInsumos.Application.Queries.ObterPecaInsumoPorId;
using PecasInsumos.Presentation.Models;

namespace PecasInsumos.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/pecas-insumos")]
public class PecasInsumosController : ControllerBase
{
    private readonly ISender _sender;

    public PecasInsumosController(ISender sender) => _sender = sender;

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] AdicionarPecaInsumoCommand command)
    {
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return CreatedAtAction(nameof(ObterPorId), new { id = result.Value.PecaInsumoId }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var result = await _sender.Send(new ListarPecasInsumosQuery());

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var result = await _sender.Send(new ObterPecaInsumoPorIdQuery(id));

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/descricao")]
    public async Task<IActionResult> AtualizarDescricao(Guid id, [FromBody] AtualizarDescricaoRequest request)
    {
        var result = await _sender.Send(new AtualizarPecaInsumoCommand(id, request.Descricao, null));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/preco")]
    public async Task<IActionResult> AtualizarPreco(Guid id, [FromBody] AtualizarPrecoRequest request)
    {
        var result = await _sender.Send(new AtualizarPecaInsumoCommand(id, null, request.Preco));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/estoque/entrada")]
    public async Task<IActionResult> IncrementarEstoque(Guid id, [FromBody] EstoqueRequest request)
    {
        var result = await _sender.Send(new IncrementarEstoqueCommand(id, request.Quantidade));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/estoque/saida")]
    public async Task<IActionResult> DecrementarEstoque(Guid id, [FromBody] EstoqueRequest request)
    {
        var result = await _sender.Send(new DecrementarEstoqueCommand(id, request.Quantidade));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var result = await _sender.Send(new DesativarPecaInsumoCommand(id));

        if (result.IsFailure)
            return NotFound(result.Error);

        return NoContent();
    }
}
