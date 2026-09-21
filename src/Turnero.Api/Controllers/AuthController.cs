using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Turnero.Api.Contracts;
using Turnero.Application.Auth;

namespace Turnero.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenResponse>> Register(CreateClientAccountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RegisterCommand(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.BirthDate);

            var result = await authService.RegisterAsync(command, cancellationToken);
            return CreatedAtAction(nameof(Register), ToResponse(result));
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
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.LoginAsync(
                new LoginCommand(request.Email, request.Password), cancellationToken);
            return Ok(ToResponse(result));
        }
        catch (InvalidCredentialsException)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciales invalidas",
                Detail = "El email o la contrasena son incorrectos."
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