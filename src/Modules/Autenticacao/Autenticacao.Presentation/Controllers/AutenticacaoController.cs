using Autenticacao.Application.Commands.Login;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autenticacao.Presentation.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/v1/auth")]
public class AutenticacaoController : ControllerBase
{
    private readonly ISender _sender;

    public AutenticacaoController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Autentica o usuário administrador e retorna um token JWT válido por 1 hora.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _sender.Send(command);

        if (result.IsFailure)
            return Unauthorized(result.Error);

        return Ok(result.Value);
    }
}
