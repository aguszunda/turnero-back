using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnero.Api.Contracts;
using Turnero.Application.Auth;

namespace Turnero.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "ADMIN")]
public sealed class UsersController(IAuthService authService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenResponse>> Create(CreateInternalUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateInternalUserCommand(
                request.Email,
                request.Password,
                request.Role,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.ProfessionalId);

            var result = await authService.CreateInternalUserAsync(command, cancellationToken);
            return CreatedAtAction(nameof(Create), ToResponse(result));
        }
        catch (EmailAlreadyRegisteredException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Email ya registrado",
                Detail = "Ya existe una cuenta con ese email."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos invalidos",
                Detail = ex.Message
            });
        }
    }

    private TokenResponse ToResponse(AuthResult result)
    {
        var usuario = new AuthUserResponse(
            result.User.Id,
            result.User.Email,
            result.User.Role,
            result.User.FirstName,
            result.User.LastName,
            result.User.ProfessionalId,
            result.User.ClientId);
        return new TokenResponse(result.Token, usuario);
    }
}