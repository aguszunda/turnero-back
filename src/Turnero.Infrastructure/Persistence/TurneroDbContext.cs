using Microsoft.EntityFrameworkCore;
using Turnero.Domain.Entities;

namespace Turnero.Infrastructure.Persistence;

public sealed class TurneroDbContext(DbContextOptions<TurneroDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<ProfessionalService> ProfessionalServices => Set<ProfessionalService>();
    public DbSet<WeeklySchedule> WeeklySchedules => Set<WeeklySchedule>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.IsActive, x.Role });
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("clientes");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(40).IsRequired();
            entity.HasIndex(x => new { x.IsActive, x.LastName });
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("services");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Price).HasPrecision(10, 2);
            entity.HasIndex(x => new { x.IsActive, x.Name });
        });

        modelBuilder.Entity<Professional>(entity =>
        {
            entity.ToTable("professionals");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.UserId);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.LastName });
        });

        modelBuilder.Entity<ProfessionalService>(entity =>
        {
            entity.ToTable("professional_services");
            entity.HasKey(x => new { x.ProfessionalId, x.ServiceId });
            entity.HasOne(x => x.Professional).WithMany(x => x.Services).HasForeignKey(x => x.ProfessionalId);
            entity.HasOne(x => x.Service).WithMany(x => x.ProfessionalServices).HasForeignKey(x => x.ServiceId);
        });

        modelBuilder.Entity<WeeklySchedule>(entity =>
        {
            entity.ToTable("weekly_schedules");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProfessionalId, x.DayOfWeek }).IsUnique();
            entity.Property(x => x.StartTime).HasColumnType("time");
            entity.Property(x => x.EndTime).HasColumnType("time");
            entity.Property(x => x.BreakStart).HasColumnType("time");
            entity.Property(x => x.BreakEnd).HasColumnType("time");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.ProfessionalId, x.StartsAt });
            entity.HasIndex(x => x.ClientId);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.AppliedPrice).HasPrecision(10, 2);
            entity.HasOne(x => x.Professional).WithMany(x => x.Appointments).HasForeignKey(x => x.ProfessionalId);
            entity.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId);
            entity.HasOne(x => x.Cliente).WithMany(x => x.Appointments).HasForeignKey(x => x.ClientId);
        });
    }
}