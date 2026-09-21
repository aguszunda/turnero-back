using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Turnero.Application.Appointments;
using Turnero.Domain.Entities;

namespace Turnero.Infrastructure.Persistence;

public sealed class AppointmentService(
    TurneroDbContext dbContext,
    IConfiguration configuration) : IAppointmentService
{
    private static readonly AppointmentStatus[] BlockingStatuses =
        [AppointmentStatus.Pending, AppointmentStatus.Reserved, AppointmentStatus.InProgress];

    public async Task<AppointmentBookingResult> CreateAsync(CreateAppointmentCommand command, CancellationToken cancellationToken)
    {
        var service = await dbContext.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == command.ServiceId && x.IsActive, cancellationToken);
        var professional = await dbContext.Professionals
            .Include(x => x.Services)
            .Include(x => x.WeeklySchedules)
            .SingleOrDefaultAsync(x => x.Id == command.ProfessionalId && x.IsActive, cancellationToken);

        if (service is null || professional is null || !professional.Services.Any(x => x.ServiceId == service.Id))
        {
            throw new InvalidAppointmentException(
                "El servicio o profesional no existe, esta inactivo o el profesional no presta ese servicio.");
        }

        if (command.StartsAt <= DateTimeOffset.UtcNow)
        {
            throw new InvalidAppointmentException("El turno debe comenzar en el futuro.");
        }

        var timeZoneId = configuration["Business:TimeZone"] ?? "America/Argentina/Buenos_Aires";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localStart = TimeZoneInfo.ConvertTime(command.StartsAt, timeZone);
        var localEnd = localStart.AddMinutes(service.DurationMinutes);
        var daySchedule = professional.WeeklySchedules.SingleOrDefault(x => x.DayOfWeek == (int)localStart.DayOfWeek);

        if (daySchedule is null || localStart.TimeOfDay < daySchedule.StartTime.ToTimeSpan() ||
            localEnd.TimeOfDay > daySchedule.EndTime.ToTimeSpan() ||
            IsInsideBreak(localStart.TimeOfDay, localEnd.TimeOfDay, daySchedule))
        {
            throw new InvalidAppointmentException("El horario solicitado esta fuera de la jornada laboral o cruza el descanso.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var candidateEnd = command.StartsAt.AddMinutes(service.DurationMinutes);
        var margin = TimeSpan.FromMinutes(daySchedule.MarginMinutes);
        var candidateStart = command.StartsAt.Subtract(margin);
        var candidateEndPlusMargin = candidateEnd.Add(margin);
        var overlap = await dbContext.Appointments.AnyAsync(x =>
            x.ProfessionalId == professional.Id &&
            BlockingStatuses.Contains(x.Status) &&
            x.StartsAt < candidateEndPlusMargin &&
            x.EndsAt > candidateStart, cancellationToken);

        if (overlap)
        {
            throw new AppointmentConflictException("El profesional ya tiene un turno en ese horario.");
        }

        var appointment = new Appointment
        {
            Code = $"TN-{localStart:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            ProfessionalId = professional.Id,
            ServiceId = service.Id,
            ClientId = command.ClientId,
            ClientName = command.ClientName.Trim(),
            ClientPhone = command.ClientPhone.Trim(),
            StartsAt = command.StartsAt.ToUniversalTime(),
            EndsAt = candidateEnd.ToUniversalTime(),
            AppliedPrice = service.Price
        };

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AppointmentBookingResult(
            appointment.Id,
            appointment.Code,
            appointment.ServiceId,
            appointment.ProfessionalId,
            appointment.ClientName,
            appointment.ClientPhone,
            appointment.StartsAt,
            appointment.EndsAt,
            appointment.Status.ToString(),
            appointment.AppliedPrice);
    }

    private static bool IsInsideBreak(TimeSpan start, TimeSpan end, WeeklySchedule schedule) =>
        schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue &&
        start < schedule.BreakEnd.Value.ToTimeSpan() && end > schedule.BreakStart.Value.ToTimeSpan();
}