
using InsuranceClaimApi.Data;
using InsuranceClaimApi.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Security.Claims;
using InsuranceClaimApi.Models;
using Microsoft.AspNetCore.Identity;
using InsuranceClaimApi.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS: Allow the deployed Angular frontend and local development.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularFrontend", policy =>
    {
        policy
            .WithOrigins(
                "https://lively-water-04b62c510.2.azurestaticapps.net",
                "http://localhost:4200"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Database connection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Controllers
builder.Services.AddControllers();
builder.Services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddHostedService<AdminBootstrapService>();
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? string.Empty;

// JWT authentication and validation
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AuthController.TokenIssuer,

            ValidateAudience = true,
            ValidAudience = AuthController.TokenAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, _, _) =>
                [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            RoleClaimType = ClaimTypes.Role
        };

        // Temporary JWT troubleshooting logs
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");

                logger.LogError(
                    context.Exception,
                    "JWT validation failed: {Error}",
                    context.Exception.Message);

                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");

                logger.LogWarning(
                    "JWT challenge: {Error}; {Description}",
                    context.Error,
                    context.ErrorDescription);

                return Task.CompletedTask;
            }
        };
    });

// Authorization
builder.Services.AddAuthorization();

// Swagger and API explorer
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InsuranceClaimApi",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT Bearer token."
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

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Middleware pipeline
app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowAngularFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
