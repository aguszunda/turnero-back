using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Turnero.Application.Appointments;
using Turnero.Application.Auth;
using Turnero.Domain.Entities;
using Turnero.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<TurneroDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

const string corsPolicy = "Angular";
builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy =>
    policy.WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Blazor UI (SSR + interactive server)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey no configurada. Ejecuta: dotnet user-secrets set \"Jwt:SigningKey\" \"...\" --project src/Turnero.Api");
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// Cookies es el esquema por defecto (UI Blazor); la API usa JWT explicitamente.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "turnero.auth";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    SeedAdministrator(app);
}

app.UseStaticFiles();
app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAntiforgery();
app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<Turnero.Api.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

static void SeedAdministrator(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TurneroDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (dbContext.Users.Any())
    {
        return;
    }

    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var seedEmail = configuration["SeedAdmin:Email"];
    var seedPassword = configuration["SeedAdmin:Password"];
    if (string.IsNullOrWhiteSpace(seedEmail) || string.IsNullOrWhiteSpace(seedPassword))
    {
        logger.LogWarning("No se pudo crear el admin inicial: faltan SeedAdmin:Email / SeedAdmin:Password.");
        return;
    }

    var admin = new User
    {
        Email = seedEmail.Trim().ToLowerInvariant(),
        Role = UserRole.ADMIN,
        FirstName = "Administrador",
        LastName = "Sistema"
    };
    admin.PasswordHash = new PasswordHasher<User>().HashPassword(admin, seedPassword);

    dbContext.Users.Add(admin);
    dbContext.SaveChanges();

    logger.LogInformation("Usuario administrador inicial creado: {Email}", admin.Email);
}

public partial class Program;