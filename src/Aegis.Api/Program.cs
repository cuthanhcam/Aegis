using Aegis.Api.Middlewares;
using Aegis.Api.Security;
using Aegis.Application;
using Aegis.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddAegisApplication();
builder.Services.AddAegisInfrastructure(builder.Configuration);

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
            RoleClaimType = "role",
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
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
