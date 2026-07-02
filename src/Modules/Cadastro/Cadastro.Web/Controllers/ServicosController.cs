using Cadastro.Application.Servicos.Commands.AdicionarServico;
using Cadastro.Application.Servicos.Commands.AtualizarServico;
using Cadastro.Application.Servicos.Commands.DesativarServico;
using Cadastro.Application.Servicos.Queries.ListarServicos;
using Cadastro.Application.Servicos.Queries.ObterServicoPorId;
using Cadastro.Web.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cadastro.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/servicos")]
public class ServicosController : ControllerBase
{
    private readonly ISender _sender;

    public ServicosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] AdicionarServicoCommand command)
    {
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return CreatedAtAction(nameof(ObterPorId), new { id = result.Value.ServicoId }, result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var result = await _sender.Send(new ListarServicosQuery());

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var result = await _sender.Send(new ObterServicoPorIdQuery(id));

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/descricao")]
    public async Task<IActionResult> AtualizarDescricao(Guid id, [FromBody] AtualizarDescricaoRequest request)
    {
        var result = await _sender.Send(new AtualizarServicoCommand(id, request.Descricao, null));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/preco")]
    public async Task<IActionResult> AtualizarPreco(Guid id, [FromBody] AtualizarPrecoRequest request)
    {
        var result = await _sender.Send(new AtualizarServicoCommand(id, null, request.Preco));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Desativar(Guid id)
    {
        var result = await _sender.Send(new DesativarServicoCommand(id));

        if (result.IsFailure)
            return NotFound(result.Error);

        return NoContent();
    }
}
