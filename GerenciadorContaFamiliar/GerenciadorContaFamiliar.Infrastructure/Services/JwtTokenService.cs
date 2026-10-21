using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GerenciadorContaFamiliar.Domain.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GerenciadorContaFamiliar.Infrastructure.Services;

/// <summary>
/// Gera tokens JWT para autenticação.
/// Configuração fica em appsettings.json → JwtSettings.
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Claim com a versão da sessão do membro (conferida a cada requisição no Program.cs).</summary>
    public const string ClaimVersaoSessao = "versao_sessao";

    public (string Token, DateTime ExpiraEm) GerarToken(int membroId, string email, int versaoSessao)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurado em appsettings.json");

        var issuer = _configuration["JwtSettings:Issuer"]
            ?? throw new InvalidOperationException("JwtSettings:Issuer não configurado.");

        var audience = _configuration["JwtSettings:Audience"]
            ?? throw new InvalidOperationException("JwtSettings:Audience não configurado.");

        var expiryMinutes = int.TryParse(_configuration["JwtSettings:ExpiryMinutes"], out var m) ? m : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiraEm = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, membroId.ToString()),
            new Claim(ClaimTypes.Name, membroId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimVersaoSessao, versaoSessao.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenString, expiraEm);
    }
}
