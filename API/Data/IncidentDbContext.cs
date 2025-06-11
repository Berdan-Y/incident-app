using System.Text.Json;
using Shared.Models.Classes;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public sealed class IncidentDbContext : DbContext
{
    public IncidentDbContext(DbContextOptions<IncidentDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Incident> Incidents { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<InvalidatedToken> InvalidatedTokens { get; set; }
    public DbSet<IncidentPhoto> IncidentPhotos { get; set; }

    // Optional: Fluent API overrides
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
    }
}
