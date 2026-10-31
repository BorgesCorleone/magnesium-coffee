using System.Security.Claims;
using System.Text;
using MagnesiumCoffee.Api.Dominio;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MagnesiumCoffee.Api.Servicos;

public sealed record JwtConfig(string Segredo, string Emissor, string Publico, int ExpiracaoHoras);

public class TokenServico(JwtConfig config)
{
    public (string Token, DateTimeOffset ExpiraEm) Gerar(Usuario usuario)
    {
        var expiraEm = DateTimeOffset.UtcNow.AddHours(config.ExpiracaoHoras);
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.Segredo));

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = config.Emissor,
            Audience = config.Publico,
            Expires = expiraEm.UtcDateTime,
            SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nome),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Papel),
            ]),
        };

        return (new JsonWebTokenHandler().CreateToken(descritor), expiraEm);
    }
}
