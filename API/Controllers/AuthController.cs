using System.Text.Json;
using API.Data;
using Shared.Models.Dtos;
using API.Helpers;
using Shared.Models.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly JwtHelper _jwtHelper;
    private readonly IncidentDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(JwtHelper jwtHelper, IncidentDbContext context, IConfiguration configuration)
    {
        _jwtHelper = jwtHelper;
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Register([FromBody] RegisterDto registerDto)
    {
        if (_context.Users.Any(u => u.Email == registerDto.Email))
            return BadRequest(new { message = "Email already exists" });

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
        var now = DateTime.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email,
            Password = hashedPassword,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.Users.Add(user);
        
        Console.WriteLine($"Role from request (raw): {registerDto.Role}");
        Console.WriteLine($"Role from request (int): {(int?)registerDto.Role}");
        Console.WriteLine($"Role from request (string): {registerDto.Role?.ToString()}");

        // Map enum values to database role IDs
        var roleId = registerDto.Role switch
        {
            Shared.Models.Enums.Role.Admin => Role.AdminId,            // Enum value 2 -> DB ID 1
            Shared.Models.Enums.Role.FieldEmployee => Role.FieldEmployeeId,  // Enum value 1 -> DB ID 2
            Shared.Models.Enums.Role.Member => Role.MemberId,          // Enum value 0 -> DB ID 3
            _ => Role.MemberId
        };

        // Also set the RoleJson property
        user.RoleJson = JsonSerializer.Serialize(new { Name = registerDto.Role?.ToString() ?? "Member" });

        Console.WriteLine($"Mapped roleId: {roleId}");
        var role = _context.Roles.Find(roleId);
        if (role == null)
        {
            Console.WriteLine($"Role not found in database for roleId: {roleId}");
            return BadRequest(new { message = "Selected role not found" });
        }
        Console.WriteLine($"Found role in database: {role.Name} (ID: {role.Id})");

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = roleId,
            User = user,
            Role = role
        };

        _context.UserRoles.Add(userRole);

        _context.SaveChanges();

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null)
            return Unauthorized(new { message = "Invalid email or password" });

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            return Unauthorized(new { message = "Invalid email or password" });

        var token = await _jwtHelper.GenerateToken(user, _context);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return Ok(new
        {
            token = token,
            userId = user.Id,
            roles = roles,
            message = "Login successful"
        });
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { message = "No token provided" });

        var invalidatedToken = new InvalidatedToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpiresInMinutes"])),
            CreatedAt = DateTime.UtcNow
        };

        _context.InvalidatedTokens.Add(invalidatedToken);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Logout successful" });
    }
}