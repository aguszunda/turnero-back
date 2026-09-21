using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Turnero.Domain.Entities;

namespace Turnero.Api.Contracts;

/// <summary>Self-service registration. Always creates a CLIENTE user.</summary>
public sealed record CreateClientAccountRequest
{
    [JsonPropertyName("nombre")]
    [Required, MaxLength(100)]
    public required string FirstName { get; init; }

    [JsonPropertyName("apellido")]
    [Required, MaxLength(100)]
    public required string LastName { get; init; }

    [JsonPropertyName("email")]
    [Required, EmailAddress, MaxLength(254)]
    public required string Email { get; init; }

    [JsonPropertyName("telefono")]
    [Required, MaxLength(40)]
    [RegularExpression(@"^[+]?[0-9\s()-]{6,}$")]
    public required string Phone { get; init; }

    [JsonPropertyName("fechaNacimiento")]
    public DateOnly? BirthDate { get; init; }

    [JsonPropertyName("password")]
    [Required, MinLength(8)]
    public required string Password { get; init; }
}

/// <summary>User login credentials.</summary>
public sealed record LoginRequest
{
    [JsonPropertyName("email")]
    [Required, EmailAddress, MaxLength(254)]
    public required string Email { get; init; }

    [JsonPropertyName("password")]
    [Required]
    public required string Password { get; init; }
}

/// <summary>Internal user creation (admin, receptionist or professional).</summary>
public sealed record CreateInternalUserRequest
{
    [JsonPropertyName("email")]
    [Required, EmailAddress, MaxLength(254)]
    public required string Email { get; init; }

    [JsonPropertyName("password")]
    [Required, MinLength(8)]
    public required string Password { get; init; }

    [JsonPropertyName("rol")]
    [Required]
    public required UserRole Role { get; init; }

    [JsonPropertyName("nombre")]
    [Required, MaxLength(100)]
    public required string FirstName { get; init; }

    [JsonPropertyName("apellido")]
    [Required, MaxLength(100)]
    public required string LastName { get; init; }

    [JsonPropertyName("telefono")]
    [MaxLength(40)]
    public string? Phone { get; init; }

    [JsonPropertyName("professionalId")]
    [Range(1, int.MaxValue)]
    public int? ProfessionalId { get; init; }
}

/// <summary>Authenticated user payload returned by login/register.</summary>
public sealed record AuthUserResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("rol")] string Role,
    [property: JsonPropertyName("nombre")] string FirstName,
    [property: JsonPropertyName("apellido")] string LastName,
    [property: JsonPropertyName("profesionalId")] int? ProfessionalId,
    [property: JsonPropertyName("clienteId")] int? ClientId);

/// <summary>Login/register response.</summary>
public sealed record TokenResponse(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("usuario")] AuthUserResponse User);