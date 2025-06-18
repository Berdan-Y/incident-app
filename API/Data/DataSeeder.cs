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
            Id = Guid.Parse("8e3c8453-2192-408b-ab06-2fb8e37a8de3"),
            Email = "admin@gmail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            FirstName = "Admin",
            LastName = "User",
            RoleJson = "{\"Name\":\"Admin\"}",
            CreatedAt = now,
            UpdatedAt = now
        };

        Console.WriteLine($"Created admin user with ID: {adminUser.Id}");

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
        
        var fieldEmployeeUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "employee@gmail.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password"),
            FirstName = "Employee",
            LastName = "Field",
            RoleJson = "{\"Name\":\"FieldEmployee\"}",
            CreatedAt = now,
            UpdatedAt = now
        };

        await _context.Users.AddRangeAsync(adminUser, testUser, fieldEmployeeUser);
        await _context.SaveChangesAsync();

        // Create UserRole entries
        var adminRole = await _context.Roles.FindAsync(Role.AdminId);
        var memberRole = await _context.Roles.FindAsync(Role.MemberId);
        var fieldEmployeeRole = await _context.Roles.FindAsync(Role.FieldEmployeeId);

        if (adminRole == null || memberRole == null || fieldEmployeeRole == null)
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
        
        var fieldEmployeeUserRole = new UserRole
        {
            UserId = fieldEmployeeUser.Id,
            RoleId = Role.FieldEmployeeId,
            User = fieldEmployeeUser,
            Role = fieldEmployeeRole
        };

        await _context.UserRoles.AddRangeAsync(adminUserRole, testUserRole, fieldEmployeeUserRole);
        await _context.SaveChangesAsync();

        // Add some test incidents
        var testIncident1 = new Incident
        {
            Id = Guid.NewGuid(),
            Title = "Test Incident 1",
            Description = "This is a test incident for the test user",
            Status = Shared.Models.Enums.Status.Todo,
            Priority = Shared.Models.Enums.Priority.Medium,
            ReportedById = adminUser.Id,
            CreatedAt = now,
            UpdatedAt = now,
            Latitude = 37.7749,
            Longitude = -122.4194,
            Address = "123 Test St, Test City",
            ZipCode = "12345"
        };

        Console.WriteLine($"Creating test incident 1 with ID: {testIncident1.Id}, ReportedById: {testIncident1.ReportedById}");

        var testIncident2 = new Incident
        {
            Id = Guid.NewGuid(),
            Title = "Test Incident 2",
            Description = "This is another test incident for the test user",
            Status = Shared.Models.Enums.Status.InProgress,
            Priority = Shared.Models.Enums.Priority.High,
            ReportedById = adminUser.Id,
            CreatedAt = now,
            UpdatedAt = now,
            Latitude = 37.7749,
            Longitude = -122.4194,
            Address = "456 Test St, Test City",
            ZipCode = "12345"
        };

        Console.WriteLine($"Creating test incident 2 with ID: {testIncident2.Id}, ReportedById: {testIncident2.ReportedById}");

        await _context.Incidents.AddRangeAsync(testIncident1, testIncident2);
        await _context.SaveChangesAsync();

        Console.WriteLine("Incidents saved to database");

        // Verify incidents were saved
        var savedIncidents = await _context.Incidents.ToListAsync();
        Console.WriteLine($"Total incidents in database: {savedIncidents.Count}");
        foreach (var incident in savedIncidents)
        {
            Console.WriteLine($"Incident {incident.Id}: Title={incident.Title}, ReportedById={incident.ReportedById}");
        }
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