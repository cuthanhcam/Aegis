using Aegis.Api.Middlewares;
using Aegis.Api.Security;
using Aegis.Application;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? "Request payload validation failed.";

            if (ErrorEnvelopePathClassifier.IsCompatibilityPath(context.HttpContext.Request.Path))
            {
                return new BadRequestObjectResult(new AegisCompatErrorResponseDto("validation_error", firstError));
            }

            return new BadRequestObjectResult(ApiResponse<string>.Fail("VALIDATION_ERROR", firstError));
        };
    });
builder.Services.AddAegisApplication();
builder.Services.AddAegisInfrastructure(builder.Configuration);

var authRatePermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:Auth:PermitLimit") ?? 10;
var authRateWindowSeconds = builder.Configuration.GetValue<int?>("RateLimiting:Auth:WindowSeconds") ?? 60;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-sensitive", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRatePermitLimit,
                Window = TimeSpan.FromSeconds(authRateWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    options.OnRejected = async (rateLimitContext, cancellationToken) =>
    {
        var response = rateLimitContext.HttpContext.Response;
        if (response.HasStarted)
        {
            return;
        }

        response.ContentType = "application/json";

        if (ErrorEnvelopePathClassifier.IsCompatibilityPath(rateLimitContext.HttpContext.Request.Path))
        {
            await response.WriteAsync(
                JsonSerializer.Serialize(new AegisCompatErrorResponseDto("rate_limit_exceeded", "Too many requests.")),
                cancellationToken);
            return;
        }

        await response.WriteAsync(
            JsonSerializer.Serialize(ApiResponse<string>.Fail("RATE_LIMIT_EXCEEDED", "Too many requests.")),
            cancellationToken);
    };
});

// CORS Configuration
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? throw new InvalidOperationException("Cors:AllowedOrigins configuration is missing.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret configuration is missing.");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RoleClaimType = ClaimTypes.Role,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Aegis",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "Aegis.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.PermissionApiAccess, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.HasClaim(claim =>
            string.Equals(claim.Type, "tenant_id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(claim.Type, "tid", StringComparison.OrdinalIgnoreCase))));

    options.AddPolicy(AuthorizationPolicies.ManagementApiAccess, policy => policy
        .RequireAuthenticatedUser()
        .RequireAssertion(context => context.User.HasClaim(claim =>
            string.Equals(claim.Type, "tenant_id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(claim.Type, "tid", StringComparison.OrdinalIgnoreCase)))
        .RequireRole("authorization_admin"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

await app.Services.InitializeAegisInfrastructureAsync(app.Configuration, app.Environment.IsDevelopment());

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware Pipeline
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("FrontendDev");
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
