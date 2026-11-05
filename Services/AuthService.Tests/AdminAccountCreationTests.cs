using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using AuthService.Dtos;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AuthService.Tests;

[Collection("AuthService integration tests")]
public sealed class AdminAccountCreationTests
{
    [Fact]
    public async Task AdminCanCreateAdminThatPersistsAndCanLogIn()
    {
        using var factory = new AuthServiceFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync(client));

        const string password = "new-admin-password-123";
        const string username = "new-reviewer";
        const string occupation = "Claims Reviewer";
        var response = await client.PostAsJsonAsync(
            "/api/auth/admin/accounts",
        new { username, password, department = occupation });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreatedAccountResponse>();
        Assert.NotNull(result);
        Assert.True(result.IsAdmin);

        var account = await factory.FindAccountAsync(username.ToUpperInvariant());
        Assert.NotNull(account);
        Assert.True(account.IsAdmin);
        Assert.Equal(occupation, account.Department);

        using var scope = factory.Services.CreateScope();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Account>>();
        Assert.Equal(
            PasswordVerificationResult.Success,
            passwordHasher.VerifyHashedPassword(account, account.PasswordHash, password));

        client.DefaultRequestHeaders.Authorization = null;
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/admin-login",
            new { username, password });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.Equal("Admin", login.Role);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.Token);
        Assert.Contains(
            token.Claims,
            claim => claim.Value == "Admin" &&
                (claim.Type == ClaimTypes.Role || claim.Type == "role"));
    }

    [CollectionDefinition("AuthService integration tests", DisableParallelization = true)]
    public sealed class AuthServiceIntegrationTestCollection
    {
    }

    [Fact]
    public async Task RejectsDuplicateNormalizedUsernames()
    {
        using var factory = new AuthServiceFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync(client));

        var response = await client.PostAsJsonAsync(
            "/api/auth/admin/accounts",
            new
            {
                username = AuthServiceFactory.ClaimantUsername.ToUpperInvariant(),
                password = "different-password-123",
        department = "Claims Review"
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task RejectsMissingDepartment(string? department)
    {
        using var factory = new AuthServiceFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync(client));

        var response = await client.PostAsJsonAsync(
            "/api/auth/admin/accounts",
            new
            {
                username = "occupation-required",
                password = "new-account-password-123",
            department
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectsOccupationLongerThanOneHundredCharacters()
    {
        using var factory = new AuthServiceFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAdminTokenAsync(client));

        var response = await client.PostAsJsonAsync(
            "/api/auth/admin/accounts",
            new
            {
                username = "occupation-too-long",
                password = "new-account-password-123",
        department = new string('x', 101)
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequiresAdminRoleForAdminAccountCreation()
    {
        using var factory = new AuthServiceFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();

        var anonymousResponse = await client.PostAsJsonAsync(
            "/api/auth/admin/accounts",
            new
            {
                username = "anonymous-admin",
                password = "new-account-password-123",
        department = "Claims Review"
            });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetClaimantTokenAsync(client));
        var claimantResponse = await client.PostAsJsonAsync(
            "/api/auth/admin/accounts",
            new
            {
                username = "claimant-created-admin",
                password = "new-account-password-123",
        department = "Claims Review"
            });
        Assert.Equal(HttpStatusCode.Forbidden, claimantResponse.StatusCode);
    }

    [Fact]
    public async Task PublicRegistrationCreatesClaimantEvenIfBodyContainsAdminFlag()
    {
        using var factory = new AuthServiceFactory();
        await factory.InitializeDatabaseAsync();
        using var client = factory.CreateClient();
        const string username = "public-claimant";

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                username,
                password = "public-account-password-123",
                isAdmin = true,
        department = "Claims Management"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var account = await factory.FindAccountAsync(username.ToUpperInvariant());
        Assert.NotNull(account);
        Assert.False(account.IsAdmin);
        Assert.Equal(string.Empty, account.Department);
    }

    private static async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/admin-login",
            new
            {
                username = AuthServiceFactory.AdminUsername,
                password = AuthServiceFactory.AdminPassword
            });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login?.Token ?? throw new InvalidOperationException("Admin login did not return a token.");
    }

    private static async Task<string> GetClaimantTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                username = AuthServiceFactory.ClaimantUsername,
                password = AuthServiceFactory.ClaimantPassword
            });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login?.Token ?? throw new InvalidOperationException("Claimant login did not return a token.");
    }
}
