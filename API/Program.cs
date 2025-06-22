using System.Text;
using API.Data;
using API.Helpers;
using API.Middleware;
using Shared.Models.Classes;
using API.Repositories;
using API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var key = Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key is not configured"));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5292", "https://localhost:5292", "http://10.0.2.2:5007")
              .SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

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

    // Add support for file uploads
    c.OperationFilter<FileUploadOperationFilter>();
});

builder.Services.AddDbContext<IncidentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<DataSeeder>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Add Incident services
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

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
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Policies for self-created incidents
    options.AddPolicy("CanViewOwnIncidents", policy =>
        policy.RequireRole(Role.Member, Role.FieldEmployee, Role.Admin));
    options.AddPolicy("CanUpdateOwnIncidents", policy =>
        policy.RequireRole(Role.Member, Role.FieldEmployee, Role.Admin));
    options.AddPolicy("CanDeleteOwnIncidents", policy =>
        policy.RequireRole(Role.Member, Role.FieldEmployee, Role.Admin));

    // Policies for all incidents
    options.AddPolicy("CanViewAllIncidents", policy =>
        policy.RequireRole(Role.FieldEmployee, Role.Admin));
    options.AddPolicy("CanUpdateIncidentStatus", policy =>
        policy.RequireRole(Role.FieldEmployee, Role.Admin));

    // Policies restricted to admins
    options.AddPolicy("CanUpdateIncidentPriority", policy =>
        policy.RequireRole(Role.Admin));
    options.AddPolicy("CanUpdateAnyIncident", policy =>
        policy.RequireRole(Role.Admin));
    options.AddPolicy("CanDeleteAnyIncident", policy =>
        policy.RequireRole(Role.Admin));
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

// Configure static files with explicit options
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot")),
    RequestPath = ""
});

// Add CORS before authentication
app.UseCors();

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
}, 500);";
    await context.Response.WriteAsync(js);
});

app.Run();
