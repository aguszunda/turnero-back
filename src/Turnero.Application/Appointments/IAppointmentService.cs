namespace Turnero.Application.Appointments;

public sealed record CreateAppointmentCommand(
    int ServiceId,
    int ProfessionalId,
    string ClientName,
    string ClientPhone,
    DateTimeOffset StartsAt,
    int? ClientId = null);

public sealed record AppointmentBookingResult(
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

public sealed class InvalidAppointmentException(string message) : Exception(message);

public sealed class AppointmentConflictException(string message) : Exception(message);

public interface IAppointmentService
{
    Task<AppointmentBookingResult> CreateAsync(CreateAppointmentCommand command, CancellationToken cancellationToken);
}