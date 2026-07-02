using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdensServico.Adapters.Controllers;
using OrdensServico.Adapters.Models.Request;

namespace OrdensServico.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/ordens-servico")]
public class OrdemServicoApiController : ControllerBase
{
    private readonly OrdemServicoController _caController;

    public OrdemServicoApiController(OrdemServicoController caController) => _caController = caController;

    [HttpPost]
    public async Task<IActionResult> Gerar([FromBody] GerarOrdemServicoRequest request)
    {
        var result = await _caController.Gerar(request);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return CreatedAtAction(nameof(ObterPorId), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var result = await _caController.ObterPorId(id);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    /// <summary>Consulta pública — permite que o cliente acompanhe o status da OS sem autenticação.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ListarPorCliente([FromQuery] Guid clienteId)
    {
        var result = await _caController.ListarPorCliente(clienteId);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/iniciar-diagnostico")]
    public async Task<IActionResult> IniciarDiagnostico(Guid id)
    {
        var result = await _caController.IniciarDiagnostico(id);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/registrar-diagnostico")]
    public async Task<IActionResult> RegistrarDiagnostico(Guid id, [FromBody] RegistrarDiagnosticoRequest request)
    {
        var result = await _caController.RegistrarDiagnostico(id, request);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/aprovar-orcamento")]
    public async Task<IActionResult> AprovarOrcamento(Guid id)
    {
        var result = await _caController.AprovarOrcamento(id);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/rejeitar-orcamento")]
    public async Task<IActionResult> RejeitarOrcamento(Guid id)
    {
        var result = await _caController.RejeitarOrcamento(id);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/executar")]
    public async Task<IActionResult> Executar(Guid id)
    {
        var result = await _caController.Executar(id);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid id)
    {
        var result = await _caController.Finalizar(id);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }

    [HttpPatch("{id}/concluir")]
    public async Task<IActionResult> Concluir(Guid id)
    {
        var result = await _caController.Concluir(id);

        if (result.IsFailure)
            return UnprocessableEntity(result.Error);

        return Ok(result.Value);
    }
}
