namespace Turnero.Domain.Entities;

public sealed class Professional
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Specialty { get; set; }
    public bool IsActive { get; set; } = true;
    public int? UserId { get; set; }
    public User? User { get; set; }
    public ICollection<ProfessionalService> Services { get; set; } = [];
    public ICollection<WeeklySchedule> WeeklySchedules { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}