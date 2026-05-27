using Autenticacao.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Autenticacao.Infrastructure.Services;

internal sealed class JwtTokenService : IJwtTokenService
{
    private static readonly TimeSpan Expiracao = TimeSpan.FromHours(1);

    private readonly string _secret;

    public JwtTokenService(IConfiguration configuration)
    {
        _secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException(
                "JWT secret não configurado.");
    }

    public TokenInfo Gerar(string email, string role)
    {
        var expiresAt = DateTime.UtcNow.Add(Expiracao);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new TokenInfo(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
