using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ClientsService.Services;

public sealed class ServiceKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ClaimsServiceKey";
    public const string HeaderName = "X-Claims-Service-Key";
    private readonly byte[] _configuredKey = Encoding.UTF8.GetBytes(
        configuration["ServiceAuth:ApiKey"]
        ?? throw new InvalidOperationException("Configure ServiceAuth:ApiKey for ClientsService."));

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var suppliedValues) ||
            suppliedValues.Count != 1 ||
            suppliedValues[0] is not { } suppliedKey)
        {
            return Task.FromResult(AuthenticateResult.Fail("A service credential is required."));
        }

        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedKey);
        if (suppliedBytes.Length != _configuredKey.Length ||
            !CryptographicOperations.FixedTimeEquals(suppliedBytes, _configuredKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("The service credential is invalid."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "ClaimsService")],
            SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
