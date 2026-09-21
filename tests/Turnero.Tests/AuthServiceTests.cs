using Microsoft.EntityFrameworkCore;
using Turnero.Application.Auth;
using Turnero.Domain.Entities;
using Turnero.Infrastructure.Persistence;

namespace Turnero.Tests;

public sealed class AuthServiceTests
{
    private static TurneroDbContext CreateContext(string name) =>
        new(new DbContextOptionsBuilder<TurneroDbContext>()
            .UseInMemoryDatabase(name)
            .Options);

    private static AuthService CreateService(TurneroDbContext db) =>
        new(db, new JwtTokenService(new JwtOptions { SigningKey = new string('y', 128) }));

    [Fact]
    public async Task Register_CreatesUserAndCliente_AndReturnsToken()
    {
        var db = CreateContext(nameof(Register_CreatesUserAndCliente_AndReturnsToken));
        var service = CreateService(db);

        var result = await service.RegisterAsync(
            new RegisterCommand("ana@test.com", "SuperClave1", "Ana", "Gomez", "+5491111111111"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("ana@test.com", result.User.Email);
        Assert.Equal("CLIENTE", result.User.Role);
        Assert.Equal("Ana", result.User.FirstName);
        Assert.Equal("Gomez", result.User.LastName);
        Assert.NotNull(result.User.ClientId);

        var stored = await db.Users.SingleAsync(u => u.Email == "ana@test.com");
        Assert.Equal(UserRole.CLIENTE, stored.Role);
        Assert.NotEqual("SuperClave1", stored.PasswordHash);
        Assert.True(await db.Clientes.AnyAsync(c => c.UserId == stored.Id));
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        var db = CreateContext(nameof(Register_DuplicateEmail_Throws));
        var service = CreateService(db);
        var command = new RegisterCommand("dup@test.com", "SuperClave1", "Ana", "Gomez", "+5491111111111");

        await service.RegisterAsync(command, CancellationToken.None);

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(
            () => service.RegisterAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResult()
    {
        var db = CreateContext(nameof(Login_WithValidCredentials_ReturnsAuthResult));
        var service = CreateService(db);
        await service.RegisterAsync(
            new RegisterCommand("lucia@test.com", "SuperClave1", "Lucia", "Paz", "+5491111111111"),
            CancellationToken.None);

        var result = await service.LoginAsync(
            new LoginCommand("Lucia@test.com", "SuperClave1"), CancellationToken.None);

        Assert.Equal("lucia@test.com", result.User.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.NotNull(result.User.ClientId);
    }

    [Fact]
    public async Task Login_WrongPassword_Throws()
    {
        var db = CreateContext(nameof(Login_WrongPassword_Throws));
        var service = CreateService(db);
        await service.RegisterAsync(
            new RegisterCommand("carlos@test.com", "SuperClave1", "Carlos", "Rios", "+5491111111111"),
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginCommand("carlos@test.com", "otra"), CancellationToken.None));
    }

    [Fact]
    public async Task Login_UnknownEmail_Throws()
    {
        var db = CreateContext(nameof(Login_UnknownEmail_Throws));
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginCommand("nadie@test.com", "SuperClave1"), CancellationToken.None));
    }

    [Fact]
    public async Task Login_InactiveUser_Throws()
    {
        var db = CreateContext(nameof(Login_InactiveUser_Throws));
        var service = CreateService(db);
        await service.RegisterAsync(
            new RegisterCommand("inac@test.com", "SuperClave1", "Ina", "Gomez", "+5491111111111"),
            CancellationToken.None);

        var stored = await db.Users.SingleAsync(u => u.Email == "inac@test.com");
        stored.IsActive = false;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => service.LoginAsync(new LoginCommand("inac@test.com", "SuperClave1"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateInternalUser_Professional_LinksProfileAndReturnsProfessionalId()
    {
        var db = CreateContext(nameof(CreateInternalUser_Professional_LinksProfileAndReturnsProfessionalId));
        var service = CreateService(db);
        var professional = new Professional { FirstName = "Ana", LastName = "Col", Specialty = "Color" };
        db.Professionals.Add(professional);
        await db.SaveChangesAsync();

        var result = await service.CreateInternalUserAsync(
            new CreateInternalUserCommand(
                "ana.prof@test.com", "ProfClave1", UserRole.PROFESIONAL, "Ana", "Col",
                ProfessionalId: professional.Id),
            CancellationToken.None);

        Assert.Equal(UserRole.PROFESIONAL.ToString(), result.User.Role);
        Assert.Equal(professional.Id, result.User.ProfessionalId);

        var stored = await db.Professionals.SingleAsync(p => p.Id == professional.Id);
        Assert.NotNull(stored.UserId);
    }

    [Fact]
    public async Task CreateInternalUser_Admin_ReturnsTokenWithoutProfile()
    {
        var db = CreateContext(nameof(CreateInternalUser_Admin_ReturnsTokenWithoutProfile));
        var service = CreateService(db);

        var result = await service.CreateInternalUserAsync(
            new CreateInternalUserCommand("admin2@test.com", "AdminClave1", UserRole.ADMIN, "Ad", "Min"),
            CancellationToken.None);

        Assert.Equal("ADMIN", result.User.Role);
        Assert.Null(result.User.ClientId);
        Assert.Null(result.User.ProfessionalId);
    }

    [Fact]
    public async Task CreateInternalUser_ClientRole_Throws()
    {
        var db = CreateContext(nameof(CreateInternalUser_ClientRole_Throws));
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateInternalUserAsync(
                new CreateInternalUserCommand("cli@test.com", "Clave123", UserRole.CLIENTE, "C", "Li"),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateInternalUser_DuplicateEmail_Throws()
    {
        var db = CreateContext(nameof(CreateInternalUser_DuplicateEmail_Throws));
        var service = CreateService(db);
        await service.CreateInternalUserAsync(
            new CreateInternalUserCommand("rec@test.com", "RecClave1", UserRole.RECEPCIONISTA, "Re", "Ce"),
            CancellationToken.None);

        await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(() =>
            service.CreateInternalUserAsync(
                new CreateInternalUserCommand("rec@test.com", "OtraClave1", UserRole.RECEPCIONISTA, "Re", "Ce"),
                CancellationToken.None));
    }
}