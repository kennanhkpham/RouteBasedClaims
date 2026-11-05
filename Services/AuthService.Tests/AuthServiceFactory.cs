using AuthService.Data;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuthService.Tests;

internal sealed class AuthServiceFactory : WebApplicationFactory<Program>
{
    public const string AdminUsername = "test-admin";
    public const string AdminPassword = "test-admin-password-123";
    public const string ClaimantUsername = "test-claimant";
    public const string ClaimantPassword = "test-claimant-password-123";

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly Dictionary<string, string?> _originalEnvironmentValues = new();

    public AuthServiceFactory()
    {
        _connection.Open();
        SetTestEnvironmentValue("ConnectionStrings__AuthDb", "Data Source=:memory:");
        SetTestEnvironmentValue("Jwt__SigningKey", "test-only-signing-key-with-at-least-32-bytes");
        SetTestEnvironmentValue("AdminBootstrap__Username", AdminUsername);
        SetTestEnvironmentValue("AdminBootstrap__Password", AdminPassword);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AuthDbContext>>();
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<AuthDbContext>();
            services.AddDbContext<AuthDbContext>(options => options.UseSqlite(_connection));

            var bootstrapService = services.SingleOrDefault(
                descriptor => descriptor.ImplementationType == typeof(AdminBootstrapService));
            if (bootstrapService is not null)
            {
                services.Remove(bootstrapService);
            }
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await context.Database.EnsureCreatedAsync();

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
        var admin = CreateAccount(AdminUsername, isAdmin: true);
        admin.PasswordHash = passwordHasher.HashPassword(admin, AdminPassword);
        var claimant = CreateAccount(ClaimantUsername, isAdmin: false);
        claimant.PasswordHash = passwordHasher.HashPassword(claimant, ClaimantPassword);

        context.Accounts.AddRange(admin, claimant);
        await context.SaveChangesAsync();
    }

    public async Task<Account?> FindAccountAsync(string normalizedUsername)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return await context.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(account => account.NormalizedUsername == normalizedUsername);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
            foreach (var (key, value) in _originalEnvironmentValues)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private void SetTestEnvironmentValue(string key, string value)
    {
        _originalEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }

    private static Account CreateAccount(string username, bool isAdmin) => new()
    {
        Username = username,
        NormalizedUsername = username.ToUpperInvariant(),
        IsAdmin = isAdmin
    };
}
