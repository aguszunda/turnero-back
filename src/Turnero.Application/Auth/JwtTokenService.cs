using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Turnero.Domain.Entities;

namespace Turnero.Application.Auth;

public sealed class JwtTokenService(JwtOptions options) : IJwtTokenService
{
    public string CreateToken(User user, AuthUser info)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, info.FirstName),
            new(ClaimTypes.GivenName, info.LastName),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        if (info.ClientId is not null)
        {
            claims.Add(new("clienteId", info.ClientId.Value.ToString()));
        }

        if (info.ProfessionalId is not null)
        {
            claims.Add(new("profesionalId", info.ProfessionalId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(options.ExpireMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}