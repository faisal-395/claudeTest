using System.Net;
using System.Net.Http.Json;
using GrainMarket.Application.Auth;
using GrainMarket.Infrastructure.Persistence;
using Xunit;

namespace GrainMarket.Api.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsTokenAndOwnerRole()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(SeedData.DefaultAdminUsername, SeedData.DefaultAdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Owner/Admin", body.RoleName);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(SeedData.DefaultAdminUsername, "totally-wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("no-such-user", "whatever"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsARefreshTokenAlongsideTheAccessToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(SeedData.DefaultAdminUsername, SeedData.DefaultAdminPassword));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.RefreshToken));
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_IssuesANewTokenPairAndRevokesTheOld()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(SeedData.DefaultAdminUsername, SeedData.DefaultAdminPassword));
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(loginBody!.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(loginBody.Token, refreshed!.Token);
        Assert.NotEqual(loginBody.RefreshToken, refreshed.RefreshToken);

        // The old refresh token was rotated out — reusing it must fail.
        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(loginBody.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest("not-a-real-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshTokenSoItCanNoLongerBeUsed()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(SeedData.DefaultAdminUsername, SeedData.DefaultAdminPassword));
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest(loginBody!.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(loginBody.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }
}
