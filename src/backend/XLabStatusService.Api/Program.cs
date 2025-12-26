using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using XLabStatusService.Api.Converters;
using XLabStatusService.Api.Filters;
using XLabStatusService.Api.Middleware;
using XLabStatusService.Application.Mappings;
using XLabStatusService.Infrastructure.Data;
using XLabStatusService.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FluentValidationFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.WriteIndented = false;
    // Добавляем конвертеры для DateTime в UTC формате ISO 8601 с "Z"
    options.JsonSerializerOptions.Converters.Add(new DateTimeUtcConverter());
    options.JsonSerializerOptions.Converters.Add(new DateTimeNullableUtcConverter());
    // Конвертер для enum в camelCase
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "X-Lab Status Service API",
        Version = "v1",
        Description = "API для мониторинга здоровья сервисов X-Lab"
    });

    // Добавляем поддержку JWT в Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// SignalR
builder.Services.AddSignalR();

// Infrastructure (Database, Repositories, Services, Quartz.NET)
builder.Services.AddInfrastructure(builder.Configuration);

// Application Services (AutoMapper, FluentValidation, AuthService, UserService)
// Note: AutoMapper 15.0.1+ requires license key. For development, you can use without license (with limitations)
// or obtain a license from https://automapper.io
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<XLabStatusService.Application.Validators.ServiceCreateDtoValidator>();
builder.Services.AddScoped<XLabStatusService.Application.Services.AuthService>();
builder.Services.AddScoped<XLabStatusService.Application.Services.UserService>();
builder.Services.AddScoped<XLabStatusService.Application.Services.ServiceService>();
builder.Services.AddScoped<XLabStatusService.Application.Services.AnalyticsService>();

// API Services
builder.Services.AddScoped<XLabStatusService.Core.Interfaces.INotificationService, XLabStatusService.Api.Services.SignalRNotificationService>();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!";
var key = Encoding.UTF8.GetBytes(secretKey);

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
        ValidIssuer = jwtSettings["Issuer"] ?? "XLabStatusService",
        ValidAudience = jwtSettings["Audience"] ?? "XLabStatusService",
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Rate Limiting
builder.Services.AddRateLimitingServices(builder.Configuration);

var app = builder.Build();

// Инициализация начальных данных (seed data)
await app.Services.SeedDatabaseAsync();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // В продакшене используем HTTPS редирект
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseMiddleware<XLabStatusService.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseMiddleware<XLabStatusService.Api.Middleware.IpBlockingMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<XLabStatusService.Api.Middleware.MaintenanceModeMiddleware>();

app.MapControllers();
app.MapHub<XLabStatusService.Api.Hubs.StatusHub>("/hubs/status");

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.Run();
