namespace Autenticacao.Application.Commands.Login;

public sealed record LoginResponse(string Token, DateTime ExpiresAt);
