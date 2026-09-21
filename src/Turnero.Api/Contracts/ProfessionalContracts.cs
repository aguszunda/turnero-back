using System.ComponentModel.DataAnnotations;

namespace Turnero.Api.Contracts;

/// <summary>Weekly working schedule for a professional.</summary>
public sealed record WeeklyScheduleRequest
{
    [Range(0, 6)]
    public int DayOfWeek { get; init; }

    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public TimeOnly? BreakStart { get; init; }
    public TimeOnly? BreakEnd { get; init; }

    [Range(0, 240)]
    public int MarginMinutes { get; init; }
}

/// <summary>Payload for creating a professional.</summary>
public sealed record CreateProfessionalRequest
{
    [Required, MaxLength(100)]
    public required string FirstName { get; init; }

    [Required, MaxLength(100)]
    public required string LastName { get; init; }

    [MaxLength(40)]
    public string? Phone { get; init; }

    [EmailAddress, MaxLength(254)]
    public string? Email { get; init; }

    [MaxLength(100)]
    public string? Specialty { get; init; }

    public List<int> ServiceIds { get; init; } = [];
    public List<WeeklyScheduleRequest> WeeklySchedules { get; init; } = [];
}

/// <summary>Professional returned by the API.</summary>
public sealed record ProfessionalResponse(
    int Id,
    string FirstName,
    string LastName,
    string? Phone,
    string? Email,
    string? Specialty,
    bool IsActive,
    IReadOnlyCollection<int> ServiceIds);