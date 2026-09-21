using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Turnero.Api.Contracts;
using Turnero.Domain.Entities;
using Turnero.Infrastructure.Persistence;

namespace Turnero.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController(
    TurneroDbContext dbContext,
    IConfiguration configuration) : ControllerBase
{
    private static readonly AppointmentStatus[] BlockingStatuses =
        [AppointmentStatus.Pending, AppointmentStatus.Reserved, AppointmentStatus.InProgress];

    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentResponse>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var service = await dbContext.Services
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.ServiceId && x.IsActive, cancellationToken);
        var professional = await dbContext.Professionals
            .Include(x => x.Services)
            .Include(x => x.WeeklySchedules)
            .SingleOrDefaultAsync(x => x.Id == request.ProfessionalId && x.IsActive, cancellationToken);

        if (service is null || professional is null || !professional.Services.Any(x => x.ServiceId == service.Id))
        {
            return BadRequest("El servicio o profesional no existe, esta inactivo o el profesional no presta ese servicio.");
        }

        if (request.StartsAt <= DateTimeOffset.UtcNow)
        {
            return BadRequest("El turno debe comenzar en el futuro.");
        }

        var timeZoneId = configuration["Business:TimeZone"] ?? "America/Argentina/Buenos_Aires";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localStart = TimeZoneInfo.ConvertTime(request.StartsAt, timeZone);
        var localEnd = localStart.AddMinutes(service.DurationMinutes);
        var daySchedule = professional.WeeklySchedules.SingleOrDefault(x => x.DayOfWeek == (int)localStart.DayOfWeek);

        if (daySchedule is null || localStart.TimeOfDay < daySchedule.StartTime.ToTimeSpan() ||
            localEnd.TimeOfDay > daySchedule.EndTime.ToTimeSpan() ||
            IsInsideBreak(localStart.TimeOfDay, localEnd.TimeOfDay, daySchedule))
        {
            return BadRequest("El horario solicitado esta fuera de la jornada laboral o cruza el descanso.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var candidateEnd = request.StartsAt.AddMinutes(service.DurationMinutes);
        var margin = TimeSpan.FromMinutes(daySchedule.MarginMinutes);
        var overlap = await dbContext.Appointments.AnyAsync(x =>
            x.ProfessionalId == professional.Id &&
            BlockingStatuses.Contains(x.Status) &&
            x.StartsAt < candidateEnd.Add(margin) &&
            x.EndsAt.Add(margin) > request.StartsAt.Subtract(margin), cancellationToken);

        if (overlap)
        {
            return Conflict("El profesional ya tiene un turno en ese horario.");
        }

        var appointment = new Appointment
        {
            Code = $"TN-{localStart:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            ProfessionalId = professional.Id,
            ServiceId = service.Id,
            ClientName = request.ClientName.Trim(),
            ClientPhone = request.ClientPhone.Trim(),
            StartsAt = request.StartsAt.ToUniversalTime(),
            EndsAt = candidateEnd.ToUniversalTime(),
            AppliedPrice = service.Price
        };

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Created($"api/appointments/{appointment.Id}", ToResponse(appointment));
    }

    private static bool IsInsideBreak(TimeSpan start, TimeSpan end, WeeklySchedule schedule) =>
        schedule.BreakStart.HasValue && schedule.BreakEnd.HasValue &&
        start < schedule.BreakEnd.Value.ToTimeSpan() && end > schedule.BreakStart.Value.ToTimeSpan();

    private static AppointmentResponse ToResponse(Appointment appointment) =>
        new(appointment.Id, appointment.Code, appointment.ServiceId, appointment.ProfessionalId,
            appointment.ClientName, appointment.ClientPhone, appointment.StartsAt, appointment.EndsAt,
            appointment.Status.ToString(), appointment.AppliedPrice);
}