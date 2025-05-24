using System.Text;
using API.Data;
using API.Helpers;
using API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key is not configured"));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Incident App API",
        Version = "v1",
        Description = "API for Incident Management System"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<IncidentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<DataSeeder>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Incident API v1");
        c.RoutePrefix = "swagger";
        
        // Add custom JavaScript to handle the token automatically
        c.InjectJavascript("/swagger-ui/custom.js");
    });
}

app.UseHttpsRedirection();

// Add JWT middleware before authentication
app.UseMiddleware<JwtMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var seeder = services.GetRequiredService<DataSeeder>();
        await seeder.SeedDataAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Add custom JavaScript file for Swagger UI
app.UseStaticFiles();
app.MapGet("/swagger-ui/custom.js", async context =>
{
    var js = @"
    setTimeout(function () {
    const ui = window.ui;

    if (!ui) {
        console.error(""Swagger UI instance (window.ui) not found."");
        return;
    }

    console.log(""Swagger UI loaded, enhancing behavior..."");

    const tryAuthorize = (token) => {
        if (ui.preauthorizeApiKey) {
            console.log('[Swagger Auth Debug] Attempting preauthorization...');
            ui.preauthorizeApiKey(""Bearer"", token);
        } else {
            console.warn('[Swagger Auth Debug] preauthorizeApiKey not available yet.');
        }
    };

    // Patch fetch
    const originalFetch = window.fetch;
    window.fetch = async function (...args) {
        const response = await originalFetch(...args);
        const url = args[0];

        if (url.includes('/api/Auth/login') && response.ok) {
            try {
                const clone = response.clone();
                const data = await clone.json();

                if (data.token) {
                    const token = 'Bearer ' + data.token;
                    console.log('[Swagger Auth Debug] Auto-authenticating with token:', token);
                    localStorage.setItem('swagger_jwt_token', token);
                    setTimeout(() => tryAuthorize(token), 200); // wait a bit more
                }
            } catch (e) {
                console.error('[Swagger Auth Debug] Error parsing response:', e);
            }
        }

        return response;
    };

    // Try preauth from saved token
    const savedToken = localStorage.getItem('swagger_jwt_token');
    if (savedToken) {
        console.log('[Swagger Auth Debug] Using saved token from localStorage');
        setTimeout(() => tryAuthorize(savedToken), 200); // delay
    }
}, 500);
";

    
    context.Response.ContentType = "application/javascript";
    await context.Response.WriteAsync(js);
});

app.Run();
