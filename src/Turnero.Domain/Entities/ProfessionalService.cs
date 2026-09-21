namespace Turnero.Domain.Entities;

public sealed class ProfessionalService
{
    public int ProfessionalId { get; set; }
    public Professional Professional { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}