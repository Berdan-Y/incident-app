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
    public DbSet<Notification> Notifications { get; set; }

    // Optional: Fluent API overrides
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());

        // Configure Notification relationships
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Incident)
            .WithMany()
            .HasForeignKey(n => n.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
