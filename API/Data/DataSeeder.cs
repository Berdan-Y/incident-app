using Shared.Models.Classes;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class DataSeeder
{
    private readonly IncidentDbContext _context;

    public DataSeeder(IncidentDbContext context)
    {
        _context = context;
    }

    public async Task SeedDataAsync()
    {
        // Clean existing data
        await CleanDataAsync();

        // Seed roles if they don't exist
        if (!await _context.Roles.AnyAsync())
        {
            var roles = new List<Role>
            {
                new() { Id = Role.AdminId, Name = Role.Admin },
                new() { Id = Role.FieldEmployeeId, Name = Role.FieldEmployee },
                new() { Id = Role.MemberId, Name = Role.Member }
            };
            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync();
        }

        var now = DateTime.UtcNow;
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@gmail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            FirstName = "Admin",
            LastName = "User",
            RoleJson = "{\"Name\":\"Admin\"}",
            CreatedAt = now,
            UpdatedAt = now
        };

        var testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@gmail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            FirstName = "Test",
            LastName = "User",
            RoleJson = "{\"Name\":\"Member\"}",
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.Users.AddRangeAsync(adminUser, testUser);
        await _context.SaveChangesAsync();

        // Create UserRole entries
        var adminRole = await _context.Roles.FindAsync(Role.AdminId);
        var memberRole = await _context.Roles.FindAsync(Role.MemberId);

        if (adminRole == null || memberRole == null)
        {
            throw new InvalidOperationException("Required roles not found in database");
        }

        var adminUserRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = Role.AdminId,
            User = adminUser,
            Role = adminRole
        };

        var testUserRole = new UserRole
        {
            UserId = testUser.Id,
            RoleId = Role.MemberId,
            User = testUser,
            Role = memberRole
        };

        await _context.UserRoles.AddRangeAsync(adminUserRole, testUserRole);
        await _context.SaveChangesAsync();
    }

    private async Task CleanDataAsync()
    {
        // Delete all data from tables in reverse order of dependencies
        _context.UserRoles.RemoveRange(await _context.UserRoles.ToListAsync());
        _context.Users.RemoveRange(await _context.Users.ToListAsync());
        _context.Roles.RemoveRange(await _context.Roles.ToListAsync());
        _context.Incidents.RemoveRange(await _context.Incidents.ToListAsync());
        _context.InvalidatedTokens.RemoveRange(await _context.InvalidatedTokens.ToListAsync());
        _context.IncidentPhotos.RemoveRange(await _context.IncidentPhotos.ToListAsync());

        await _context.SaveChangesAsync();
    }
}