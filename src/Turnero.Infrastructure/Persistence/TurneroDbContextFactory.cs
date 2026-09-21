using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Turnero.Infrastructure.Persistence;

public sealed class TurneroDbContextFactory : IDesignTimeDbContextFactory<TurneroDbContext>
{
    public TurneroDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets("Turnero.Api-Development")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection no configurada. Ejecuta: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" ... --project src/Turnero.Api");

        var optionsBuilder = new DbContextOptionsBuilder<TurneroDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new TurneroDbContext(optionsBuilder.Options);
    }
}