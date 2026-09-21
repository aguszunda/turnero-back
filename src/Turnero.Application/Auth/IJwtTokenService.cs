using Turnero.Domain.Entities;

namespace Turnero.Application.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Turnero.Api";
    public string Audience { get; set; } = "Turnero.Frontend";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 480;
}

public interface IJwtTokenService
{
    string CreateToken(User user, AuthUser info);
}