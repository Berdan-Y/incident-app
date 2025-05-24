using System.IdentityModel.Tokens.Jwt;
using System.Text;
using API.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace API.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public JwtMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task Invoke(HttpContext context, IncidentDbContext dbContext)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null)
        {
            // Check if token is in the invalidated tokens list
            var isInvalidated = await dbContext.InvalidatedTokens
                .AnyAsync(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow);

            if (isInvalidated)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { message = "Token has been invalidated" });
                return;
            }
        }

        await _next(context);
    }
}