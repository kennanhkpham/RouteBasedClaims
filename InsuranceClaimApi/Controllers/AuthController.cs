using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InsuranceClaimApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public const string TokenIssuer = "RouteBasedClaim";
    public const string TokenAudience = "RouteBasedClaimAdmin";
    public const string SigningKey = "RouteBasedClaim-Demo-Jwt-Signing-Key-Change-Before-Production-2026!";

    private const string AdminUsername = "admin";
    private const string AdminPassword = "adminpassword";

    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (request.Username != AdminUsername || request.Password != AdminPassword)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var expiresAt = DateTime.UtcNow.AddHours(1);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, AdminUsername),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TokenIssuer,
            audience: TokenAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt));
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token, DateTime ExpiresAt);
