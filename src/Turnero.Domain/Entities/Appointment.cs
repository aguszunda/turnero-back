namespace Turnero.Domain.Entities;

public sealed class Appointment
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int? ClientId { get; set; }
    public Cliente? Cliente { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Reserved;
    public decimal AppliedPrice { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum AppointmentStatus
{
    Pending,
    Reserved,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}