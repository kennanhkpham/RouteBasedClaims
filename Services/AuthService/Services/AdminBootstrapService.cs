using System.Text;
using System.Text.RegularExpressions;
using AuthService.Data;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public sealed class AdminBootstrapService : IHostedService
{
    private static readonly Regex UsernamePattern = new("^[A-Za-z0-9._-]+$", RegexOptions.Compiled);

    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminBootstrapService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var signingKey = _configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Configure Jwt:SigningKey with at least 32 bytes.");
        }

        var username = _configuration["AdminBootstrap:Username"]?.Trim();
        var password = _configuration["AdminBootstrap:Password"];
        if (string.IsNullOrWhiteSpace(username) ||
            username.Length is < 3 or > 50 ||
            !UsernamePattern.IsMatch(username) ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length < 12)
        {
            throw new InvalidOperationException(
                "Configure a valid AdminBootstrap:Username and a password of at least 12 characters.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
        var normalizedUsername = username.ToUpperInvariant();
        var existingAccount = await dbContext.Accounts
            .SingleOrDefaultAsync(
                account => account.NormalizedUsername == normalizedUsername,
                cancellationToken);

        if (existingAccount is null)
        {
            var administrator = new Account
            {
                Username = username,
                NormalizedUsername = normalizedUsername,
                IsAdmin = true,
                Department = "Administration"
            };
            administrator.PasswordHash = passwordHasher.HashPassword(administrator, password);
            dbContext.Accounts.Add(administrator);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (!existingAccount.IsAdmin)
        {
            throw new InvalidOperationException(
                "The configured administrator username is already assigned to a non-admin account.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
