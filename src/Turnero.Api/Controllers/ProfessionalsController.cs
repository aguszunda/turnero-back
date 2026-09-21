using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Turnero.Api.Contracts;
using Turnero.Domain.Entities;
using Turnero.Infrastructure.Persistence;

namespace Turnero.Api.Controllers;

[ApiController]
[Route("api/professionals")]
public sealed class ProfessionalsController(TurneroDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProfessionalResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var professionals = await dbContext.Professionals
            .AsNoTracking()
            .Include(x => x.Services)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new ProfessionalResponse(
                x.Id, x.FirstName, x.LastName, x.Phone, x.Email, x.Specialty, x.IsActive,
                x.Services.Select(service => service.ServiceId).ToArray()))
            .ToListAsync(cancellationToken);

        return Ok(professionals);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProfessionalResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProfessionalResponse>> Create(CreateProfessionalRequest request, CancellationToken cancellationToken)
    {
        if (request.WeeklySchedules.GroupBy(x => x.DayOfWeek).Any(group => group.Count() > 1))
        {
            return BadRequest("Solo se permite un horario por dia para cada profesional.");
        }

        var services = await dbContext.Services
            .Where(x => request.ServiceIds.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);

        if (services.Count != request.ServiceIds.Distinct().Count())
        {
            return BadRequest("Uno o mas servicios no existen o estan inactivos.");
        }

        var schedules = request.WeeklySchedules.Select(schedule => new WeeklySchedule
        {
            DayOfWeek = schedule.DayOfWeek,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            BreakStart = schedule.BreakStart,
            BreakEnd = schedule.BreakEnd,
            MarginMinutes = schedule.MarginMinutes
        }).ToList();

        if (schedules.Any(x => x.StartTime >= x.EndTime ||
            (x.BreakStart.HasValue && x.BreakEnd.HasValue &&
             (x.BreakStart >= x.BreakEnd || x.BreakStart < x.StartTime || x.BreakEnd > x.EndTime))))
        {
            return BadRequest("Los horarios laborales o de descanso no son validos.");
        }

        var professional = new Professional
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = request.Phone,
            Email = request.Email,
            Specialty = request.Specialty,
            Services = services.Select(service => new ProfessionalService { Service = service }).ToList(),
            WeeklySchedules = schedules
        };

        dbContext.Professionals.Add(professional);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = professional.Id }, ToResponse(professional));
    }

    private static ProfessionalResponse ToResponse(Professional professional) =>
        new(professional.Id, professional.FirstName, professional.LastName, professional.Phone,
            professional.Email, professional.Specialty, professional.IsActive,
            professional.Services.Select(x => x.ServiceId).ToArray());
}