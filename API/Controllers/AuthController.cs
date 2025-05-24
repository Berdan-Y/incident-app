using System.Text.Json;
using API.Data;
using API.Dtos;
using API.Helpers;
using API.Models.Classes;
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
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = registerDto.Email,
            Password = hashedPassword,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
        };
        
        _context.Users.Add(user);

        var role = _context.Roles.Find(Role.MemberId);
        if (role == null)
            return BadRequest(new { message = "Default role not found" });

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = Role.MemberId,
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
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null)
            return Unauthorized(new { message = "Invalid email or password" });

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.Password))
            return Unauthorized(new { message = "Invalid email or password" });

        var token = await _jwtHelper.GenerateToken(user, _context);
        
        return Ok(new { 
            token = token,
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