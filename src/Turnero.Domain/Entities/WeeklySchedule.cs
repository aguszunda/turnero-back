namespace Turnero.Domain.Entities;

public sealed class WeeklySchedule
{
    public int Id { get; set; }
    public int ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;
    public int DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly? BreakStart { get; set; }
    public TimeOnly? BreakEnd { get; set; }
    public int MarginMinutes { get; set; }
}