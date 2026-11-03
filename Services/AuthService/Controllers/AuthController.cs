using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Data;
using AuthService.Dtos;
using AuthService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    AuthDbContext context,
    IPasswordHasher<Account> passwordHasher,
    IConfiguration configuration) : ControllerBase
{
    public const string TokenIssuer = "RouteBasedClaim";
    public const string TokenAudience = "RouteBasedClaimAdmin";

    private readonly string _signingKey = GetSigningKey(configuration);

    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalizedUsername = NormalizeUsername(username);
        var usernameExists = await context.Accounts
            .AnyAsync(account => account.NormalizedUsername == normalizedUsername, cancellationToken);

        if (usernameExists)
        {
            return Conflict(new { message = "An account with that username already exists." });
        }

        var account = new Account
        {
            Username = username,
            NormalizedUsername = normalizedUsername
        };
        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);

        context.Accounts.Add(account);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await context.Accounts.AnyAsync(
                    item => item.NormalizedUsername == normalizedUsername,
                    cancellationToken))
            {
                throw;
            }

            return Conflict(new { message = "An account with that username already exists." });
        }

        return Ok(CreateToken(account));
    }

    [HttpPost("accounts")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CreatedAccountResponse>> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalizedUsername = NormalizeUsername(username);
        var usernameExists = await context.Accounts
            .AnyAsync(account => account.NormalizedUsername == normalizedUsername, cancellationToken);

        if (usernameExists)
        {
            return Conflict(new { message = "An account with that username already exists." });
        }

        var account = new Account
        {
            Username = username,
            NormalizedUsername = normalizedUsername,
            IsAdmin = request.IsAdmin
        };
        account.PasswordHash = passwordHasher.HashPassword(account, request.Password);

        context.Accounts.Add(account);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await context.Accounts.AnyAsync(
                    item => item.NormalizedUsername == normalizedUsername,
                    cancellationToken))
            {
                throw;
            }

            return Conflict(new { message = "An account with that username already exists." });
        }

        return Created(
            $"/api/auth/accounts/{account.Id}",
            new CreatedAccountResponse(account.Id, account.Username, account.IsAdmin));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(request.Username, cancellationToken);
        if (account is null ||
            !VerifyPassword(account, request.Password, out var rehashNeeded) ||
            account.IsAdmin)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        await RehashPasswordIfNeededAsync(account, request.Password, rehashNeeded, cancellationToken);

        return Ok(CreateToken(account));
    }

    [HttpPost("admin-login")]
    public async Task<ActionResult<LoginResponse>> AdminLogin(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(request.Username, cancellationToken);
        if (account is null ||
            !VerifyPassword(account, request.Password, out var rehashNeeded) ||
            !account.IsAdmin)
        {
            return Unauthorized(new { message = "Invalid administrator username or password." });
        }

        await RehashPasswordIfNeededAsync(account, request.Password, rehashNeeded, cancellationToken);

        return Ok(CreateToken(account));
    }

    private async Task<Account?> FindAccountAsync(
        string username,
        CancellationToken cancellationToken)
    {
        var normalizedUsername = NormalizeUsername(username.Trim());
        return await context.Accounts.SingleOrDefaultAsync(
            account => account.NormalizedUsername == normalizedUsername,
            cancellationToken);
    }

    private bool VerifyPassword(Account account, string password, out bool rehashNeeded)
    {
        var result = passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password);
        rehashNeeded = result == PasswordVerificationResult.SuccessRehashNeeded;
        return result != PasswordVerificationResult.Failed;
    }

    private async Task RehashPasswordIfNeededAsync(
        Account account,
        string password,
        bool rehashNeeded,
        CancellationToken cancellationToken)
    {
        if (!rehashNeeded)
        {
            return;
        }

        account.PasswordHash = passwordHasher.HashPassword(account, password);
        await context.SaveChangesAsync(cancellationToken);
    }

    private LoginResponse CreateToken(Account account)
    {
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Username),
            new Claim(ClaimTypes.Role, account.IsAdmin ? "Admin" : "Claimant")
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TokenIssuer,
            audience: TokenAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            account.IsAdmin ? "Admin" : "Claimant");
    }

    private static string GetSigningKey(IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("JWT signing key configuration is required.");

        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 bytes.");
        }

        return signingKey;
    }

    private static string NormalizeUsername(string username) => username.ToUpperInvariant();
}

public sealed class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$")]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; init; } = string.Empty;
}

public sealed class CreateAccountRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$")]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public string Password { get; init; } = string.Empty;

    public bool IsAdmin { get; init; }
}
