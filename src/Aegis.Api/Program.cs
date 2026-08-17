using Aegis.Api.Middlewares;
using Aegis.Api.Security;
using Aegis.Api.Health;
using Aegis.Api.Metrics;
using Aegis.Application;
using Aegis.Authorization.Core.Metrics;
using Aegis.Contracts.Common;
using Aegis.Contracts.Compatibility;
using Aegis.Infrastructure;
using Aegis.SharedKernel.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    throw new InvalidOperationException("Jwt:Secret configuration is missing.");
}

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
var httpLoggingEnabled = builder.Configuration.GetSection("Logging:Http").GetValue<bool>("Enabled");
if (httpLoggingEnabled)
{
    builder.Services.AddHttpLogging(options =>
    {
        options.LoggingFields = HttpLoggingFields.RequestMethod
            | HttpLoggingFields.RequestPath
            | HttpLoggingFields.ResponseStatusCode
            | HttpLoggingFields.Duration;
    });
}
builder.Services.AddOptions<AuthOptions>()
    .BindConfiguration("Auth")
    .Validate(options => options.DemoUsers is { Count: > 0 }, "Auth:DemoUsers configuration is missing.")
    .ValidateOnStart();
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Secret), "Jwt:Secret configuration is missing.")
    .ValidateOnStart();

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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RoleClaimType = ClaimTypes.Role,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
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
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aegis API",
        Version = "v1",
        Description = "Centralized, explainable authorization API.",
    });
    options.AddServer(new OpenApiServer { Url = "/" });
    options.CustomSchemaIds(ContractSchemaId);

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

// Health checks
var healthChecks = builder.Services.AddHealthChecks();
var storageProvider = builder.Configuration.GetSection("Storage").GetValue<string>("Provider") ?? "InMemory";
if (storageProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
{
    healthChecks.AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);
}

var cacheProvider = builder.Configuration.GetSection("Cache").GetValue<string>("Provider") ?? "Memory";
if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
{
    healthChecks.AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
}

var app = builder.Build();

await app.Services.InitializeAegisInfrastructureAsync(app.Configuration);

if (args.Any(arg => string.Equals(arg, "--migrate-only", StringComparison.OrdinalIgnoreCase)))
{
    return;
}

if (app.Environment.IsDevelopment())
{
    if (httpLoggingEnabled)
    {
        app.UseHttpLogging();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware Pipeline
app.UseCors("FrontendDev");
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthorization();

// Liveness & readiness endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapGet("/metrics", (IAuthorizationMetrics metrics) =>
    Results.Text(PrometheusMetricsFormatter.Format(metrics), PrometheusMetricsFormatter.ContentType));

app.MapControllers();

app.Run();

static string ContractSchemaId(Type type)
{
    if (type == typeof(ApiError))
    {
        return "AegisApiError";
    }

    return type.IsConstructedGenericType
        ? string.Concat(type.GetGenericArguments().Select(ContractSchemaId)) + type.Name.Split('`')[0]
        : type.Name.Replace("[]", "Array", StringComparison.Ordinal);
}

public partial class Program;
