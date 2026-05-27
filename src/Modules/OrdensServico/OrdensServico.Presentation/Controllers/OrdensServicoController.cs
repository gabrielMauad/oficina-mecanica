using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrdensServico.Application.Ordens.Commands.AprovarOrcamento;
using OrdensServico.Application.Ordens.Commands.ConcluirOrdemServico;
using OrdensServico.Application.Ordens.Commands.ExecutarOrdemServico;
using OrdensServico.Application.Ordens.Commands.FinalizarOrdemServico;
using OrdensServico.Application.Ordens.Commands.GerarOrdemServico;
using OrdensServico.Application.Ordens.Commands.IniciarDiagnostico;
using OrdensServico.Application.Ordens.Commands.RegistrarDiagnostico;
using OrdensServico.Application.Ordens.Commands.RejeitarOrcamento;
using OrdensServico.Application.Ordens.Queries.ListarOrdensPorCliente;
using OrdensServico.Application.Ordens.Queries.ObterOrdemServicoPorId;
using OrdensServico.Presentation.Models;

namespace OrdensServico.Presentation.Controllers;

[ApiController]
[Route("api/v1/ordens-servico")]
public class OrdensServicoController : ControllerBase
{
    private readonly ISender _sender;

    public OrdensServicoController(ISender sender) => _sender = sender;

    [HttpPost]
    public async Task<IActionResult> Gerar([FromBody] GerarOrdemServicoCommand command)
    {
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return CreatedAtAction(nameof(ObterPorId), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var result = await _sender.Send(new ObterOrdemServicoPorIdQuery(id));

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> ListarPorCliente([FromQuery] Guid clienteId)
    {
        var result = await _sender.Send(new ListarOrdensPorClienteQuery(clienteId));

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/iniciar-diagnostico")]
    public async Task<IActionResult> IniciarDiagnostico(Guid id)
    {
        var result = await _sender.Send(new IniciarDiagnosticoCommand(id));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/registrar-diagnostico")]
    public async Task<IActionResult> RegistrarDiagnostico(Guid id, [FromBody] RegistrarDiagnosticoRequest request)
    {
        var command = new RegistrarDiagnosticoCommand(id, request.DescricaoDiagnostico, request.Servicos, request.Pecas);
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/aprovar-orcamento")]
    public async Task<IActionResult> AprovarOrcamento(Guid id)
    {
        var result = await _sender.Send(new AprovarOrcamentoCommand(id));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/rejeitar-orcamento")]
    public async Task<IActionResult> RejeitarOrcamento(Guid id)
    {
        var result = await _sender.Send(new RejeitarOrcamentoCommand(id));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/executar")]
    public async Task<IActionResult> Executar(Guid id)
    {
        var result = await _sender.Send(new ExecutarOrdemServicoCommand(id));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid id)
    {
        var result = await _sender.Send(new FinalizarOrdemServicoCommand(id));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/concluir")]
    public async Task<IActionResult> Concluir(Guid id)
    {
        var result = await _sender.Send(new ConcluirOrdemServicoCommand(id));

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }
}
