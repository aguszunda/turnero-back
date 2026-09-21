using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Turnero.Api.Contracts;
using Turnero.Domain.Entities;
using Turnero.Infrastructure.Persistence;

namespace Turnero.Api.Controllers;

[ApiController]
[Route("api/services")]
public sealed class ServicesController(TurneroDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ServiceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ServiceResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var services = await dbContext.Services
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ServiceResponse(x.Id, x.Name, x.Description, x.DurationMinutes, x.Price, x.Category, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(services);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceResponse>> Create(CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var service = new Service
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            Category = request.Category
        };

        dbContext.Services.Add(service);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = ToResponse(service);
        return CreatedAtAction(nameof(GetAll), response);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceResponse>> Update(int id, UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var service = await dbContext.Services.FindAsync([id], cancellationToken);
        if (service is null)
        {
            return NotFound();
        }

        service.Name = request.Name.Trim();
        service.Description = request.Description;
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.Category = request.Category;
        service.IsActive = request.IsActive;
        service.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToResponse(service));
    }

    private static ServiceResponse ToResponse(Service service) =>
        new(service.Id, service.Name, service.Description, service.DurationMinutes, service.Price, service.Category, service.IsActive);
}