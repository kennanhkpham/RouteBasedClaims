using System.Security.Claims;
using System.Text;
using ClaimsService.Data;
using ClaimsService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ClaimsDb")
    ?? throw new InvalidOperationException(
        "Configure ConnectionStrings:ClaimsDb for ClaimsService.");
var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Configure Jwt:SigningKey for ClaimsService.");
var clientsServiceBaseUrl = builder.Configuration["ClientsService:BaseUrl"]
    ?? throw new InvalidOperationException("Configure ClientsService:BaseUrl for ClaimsService.");
var clientsServiceApiKey = builder.Configuration["ClientsService:ApiKey"]
    ?? throw new InvalidOperationException("Configure ClientsService:ApiKey for ClaimsService.");

if (Encoding.UTF8.GetByteCount(signingKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
}

if (Encoding.UTF8.GetByteCount(clientsServiceApiKey) < 32)
{
    throw new InvalidOperationException("ClientsService:ApiKey must be at least 32 bytes.");
}

if (!Uri.TryCreate(clientsServiceBaseUrl, UriKind.Absolute, out var clientsServiceUri) ||
    (clientsServiceUri.Scheme != Uri.UriSchemeHttp &&
     clientsServiceUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException("ClientsService:BaseUrl must be an absolute HTTP(S) URL.");
}

if (!clientsServiceUri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
{
    clientsServiceUri = new Uri($"{clientsServiceUri.AbsoluteUri}/");
}

builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServer => sqlServer.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: [-2])));
builder.Services.AddScoped<IClientProfileOutboxStore, ClientProfileOutboxStore>();
builder.Services.AddHttpClient("clients-service", client =>
{
    client.BaseAddress = clientsServiceUri;
    client.DefaultRequestHeaders.Add(
        "X-Claims-Service-Key",
        clientsServiceApiKey);
});
builder.Services.AddHostedService<ClientProfileOutboxDispatcher>();
builder.Services.AddControllers();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "RouteBasedClaim",
            ValidateAudience = true,
            ValidAudience = "RouteBasedClaimAdmin",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ClaimsService",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
