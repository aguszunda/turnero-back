using Microsoft.AspNetCore.Mvc;
using Turnero.Api.Contracts;
using Turnero.Application.Appointments;

namespace Turnero.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentResponse>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await appointmentService.CreateAsync(
                new CreateAppointmentCommand(
                    request.ServiceId,
                    request.ProfessionalId,
                    request.ClientName,
                    request.ClientPhone,
                    request.StartsAt),
                cancellationToken);

            return Created($"api/appointments/{result.Id}", ToResponse(result));
        }
        catch (InvalidAppointmentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos invalidos",
                Detail = ex.Message
            });
        }
        catch (AppointmentConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflicto de agenda",
                Detail = ex.Message
            });
        }
    }

    private static AppointmentResponse ToResponse(AppointmentBookingResult result) =>
        new(result.Id, result.Code, result.ServiceId, result.ProfessionalId,
            result.ClientName, result.ClientPhone, result.StartsAt, result.EndsAt,
            result.Status, result.AppliedPrice);
}