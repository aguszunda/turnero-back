using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Turnero.Application.Auth;
using Turnero.Domain.Entities;

namespace Turnero.Infrastructure.Persistence;

public sealed class AuthService(
    TurneroDbContext dbContext,
    IJwtTokenService tokenService) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<AuthResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(command.Email);
        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new EmailAlreadyRegisteredException();
        }

        var user = new User
        {
            Email = email,
            Role = UserRole.CLIENTE,
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim()
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, command.Password);

        var cliente = new Cliente
        {
            User = user,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = command.Phone.Trim(),
            BirthDate = command.BirthDate
        };

        dbContext.Users.Add(user);
        dbContext.Clientes.Add(cliente);
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildResult(user, cliente.Id, professionalId: null);
    }

    public async Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(command.Email);
        var user = await dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !user.IsActive ||
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.Password) != PasswordVerificationResult.Success)
        {
            throw new InvalidCredentialsException();
        }

        return await BuildResultAsync(user, cancellationToken);
    }

    public async Task<AuthResult> CreateInternalUserAsync(CreateInternalUserCommand command, CancellationToken cancellationToken)
    {
        if (command.Role == UserRole.CLIENTE)
        {
            throw new InvalidOperationException("Los usuarios CLIENTE se crean en /auth/register.");
        }

        var email = NormalizeEmail(command.Email);
        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new EmailAlreadyRegisteredException();
        }

        var user = new User
        {
            Email = email,
            Role = command.Role,
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim()
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, command.Password);

        int? professionalId = null;
        if (command.Role == UserRole.PROFESIONAL)
        {
            if (command.ProfessionalId is null)
            {
                throw new InvalidOperationException("Para el rol PROFESIONAL se requiere professionalId.");
            }

            var professional = await dbContext.Professionals.SingleOrDefaultAsync(
                x => x.Id == command.ProfessionalId.Value, cancellationToken);
            if (professional is null)
            {
                throw new InvalidOperationException("El profesional no existe.");
            }

            if (professional.UserId is not null)
            {
                throw new InvalidOperationException("El profesional ya tiene un usuario asociado.");
            }

            professional.UserId = user.Id;
            professional.User = user;
            professionalId = professional.Id;
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildResult(user, clientId: null, professionalId);
    }

    private async Task<AuthResult> BuildResultAsync(User user, CancellationToken cancellationToken)
    {
        int? clientId = null;
        int? professionalId = null;

        if (user.Role == UserRole.CLIENTE)
        {
            var cliente = await dbContext.Clientes.AsNoTracking()
                .SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
            clientId = cliente?.Id;
            if (cliente is not null)
            {
                user.FirstName = cliente.FirstName;
                user.LastName = cliente.LastName;
            }
        }
        else if (user.Role == UserRole.PROFESIONAL)
        {
            var professional = await dbContext.Professionals.AsNoTracking()
                .SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
            professionalId = professional?.Id;
        }

        return BuildResult(user, clientId, professionalId);
    }

    private AuthResult BuildResult(User user, int? clientId, int? professionalId)
    {
        var info = new AuthUser(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.FirstName,
            user.LastName,
            professionalId,
            clientId);

        return new AuthResult(tokenService.CreateToken(user, info), info);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}