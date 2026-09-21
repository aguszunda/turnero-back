using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Turnero.Application.Auth;
using Turnero.Domain.Entities;

namespace Turnero.Tests;

public sealed class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Issuer = "Test.Issuer",
        Audience = "Test.Audience",
        SigningKey = new string('x', 128),
        ExpireMinutes = 60
    };

    [Fact]
    public void CreateToken_IncludesExpectedClaims()
    {
        var service = new JwtTokenService(Options);
        var user = new User { Id = 42, Email = "cliente@test.com", Role = UserRole.CLIENTE };
        var info = new AuthUser(42, "cliente@test.com", "CLIENTE", "Ana", "Gomez", null, 7);

        var token = service.CreateToken(user, info);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("42", jwt.Payload.Sub);
        Assert.Equal(Options.Issuer, jwt.Issuer);
        Assert.Equal(Options.Audience, jwt.Audiences.Single());
        Assert.Equal("cliente@test.com", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("CLIENTE", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("7", jwt.Claims.Single(c => c.Type == "clienteId").Value);
    }

    [Fact]
    public void CreateToken_IncludesProfessionalId_WhenProfessional()
    {
        var service = new JwtTokenService(Options);
        var user = new User { Id = 3, Email = "prof@test.com", Role = UserRole.PROFESIONAL };
        var info = new AuthUser(3, "prof@test.com", "PROFESIONAL", "Ana", "Col", 11, null);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateToken(user, info));

        Assert.Equal("PROFESIONAL", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("11", jwt.Claims.Single(c => c.Type == "profesionalId").Value);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "clienteId");
    }

    [Fact]
    public void CreateToken_ExpiresAccordingToConfiguredMinutes()
    {
        var service = new JwtTokenService(Options);
        var user = new User { Id = 1, Email = "a@test.com", Role = UserRole.ADMIN };
        var info = new AuthUser(1, "a@test.com", "ADMIN", "A", "B", null, null);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateToken(user, info));

        var lifetime = jwt.Payload.Expiration!.Value - jwt.Payload.NotBefore!.Value;
        Assert.Equal(3600, lifetime);
    }
}