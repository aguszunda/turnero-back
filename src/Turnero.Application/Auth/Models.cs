using Turnero.Domain.Entities;

namespace Turnero.Application.Auth;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Phone,
    DateOnly? BirthDate = null);

public sealed record LoginCommand(string Email, string Password);

public sealed record CreateInternalUserCommand(
    string Email,
    string Password,
    UserRole Role,
    string FirstName,
    string LastName,
    string? Phone = null,
    int? ProfessionalId = null);

public sealed record AuthUser(
    int Id,
    string Email,
    string Role,
    string FirstName,
    string LastName,
    int? ProfessionalId,
    int? ClientId);

public sealed record AuthResult(string Token, AuthUser User);