namespace Turnero.Application.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<AuthUser> ValidateAsync(LoginCommand command, CancellationToken cancellationToken);
    Task<AuthResult> CreateInternalUserAsync(CreateInternalUserCommand command, CancellationToken cancellationToken);
}