using System.ComponentModel.DataAnnotations;

namespace Turnero.Api.Contracts;

/// <summary>Payload for booking an appointment.</summary>
public sealed record CreateAppointmentRequest
{
    [Range(1, int.MaxValue)]
    public int ServiceId { get; init; }

    [Range(1, int.MaxValue)]
    public int ProfessionalId { get; init; }

    [Required, MaxLength(200)]
    public required string ClientName { get; init; }

    [Required, MaxLength(40)]
    public required string ClientPhone { get; init; }

    public DateTimeOffset StartsAt { get; init; }
}

/// <summary>Appointment returned by the API.</summary>
public sealed record AppointmentResponse(
    int Id,
    string Code,
    int ServiceId,
    int ProfessionalId,
    string ClientName,
    string ClientPhone,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status,
    decimal AppliedPrice);