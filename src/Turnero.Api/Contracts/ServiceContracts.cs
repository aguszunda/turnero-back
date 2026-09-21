using System.ComponentModel.DataAnnotations;

namespace Turnero.Api.Contracts;

/// <summary>Payload for creating a service.</summary>
public sealed record CreateServiceRequest
{
    [Required, MaxLength(150)]
    public required string Name { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Range(1, 1440)]
    public int DurationMinutes { get; init; }

    [Range(0, 999999.99)]
    public decimal Price { get; init; }

    [MaxLength(100)]
    public string? Category { get; init; }
}

/// <summary>Payload for updating a service.</summary>
public sealed record UpdateServiceRequest
{
    [Required, MaxLength(150)]
    public required string Name { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Range(1, 1440)]
    public int DurationMinutes { get; init; }

    [Range(0, 999999.99)]
    public decimal Price { get; init; }

    [MaxLength(100)]
    public string? Category { get; init; }

    public bool IsActive { get; init; }
}

/// <summary>Service returned by the API.</summary>
public sealed record ServiceResponse(
    int Id,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string? Category,
    bool IsActive);