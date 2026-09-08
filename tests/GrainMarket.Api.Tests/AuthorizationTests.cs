using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GrainMarket.Api.Tests;

public class AuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetChartOfAccounts_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/chart-of-accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetParties_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/parties");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_AsClerk_ReturnsForbidden()
    {
        // Clerk has no permission on the SetupUsersRoles module.
        var clerk = await TestAuthHelper.AsNewUserWithRoleAsync(_factory, "Clerk", "clerk.forbidden.test");

        var response = await clerk.PostAsJsonAsync("/api/users",
            new { username = "should-not-be-created", fullName = "x", password = "Password@123", roleId = 1, isActive = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
