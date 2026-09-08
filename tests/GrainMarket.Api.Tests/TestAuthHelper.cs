using System.Net.Http.Headers;
using System.Net.Http.Json;
using GrainMarket.Application.Auth;
using GrainMarket.Application.Roles;
using GrainMarket.Application.Users;
using GrainMarket.Infrastructure.Persistence;

namespace GrainMarket.Api.Tests;

internal static class TestAuthHelper
{
    public static async Task<HttpClient> AsAdminAsync(CustomWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(SeedData.DefaultAdminUsername, SeedData.DefaultAdminPassword));
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    /// <summary>Creates a fresh user under the given seeded role name (e.g. "Manager", "Clerk") and returns an authenticated client for them.</summary>
    public static async Task<HttpClient> AsNewUserWithRoleAsync(CustomWebApplicationFactory factory, string roleName, string username, string password = "Password@123")
    {
        var admin = await AsAdminAsync(factory);

        var roles = await admin.GetFromJsonAsync<List<RoleDto>>("/api/roles");
        var role = roles!.Single(r => r.Name == roleName);

        var createResponse = await admin.PostAsJsonAsync("/api/users",
            new CreateUserRequest(username, username, password, role.Id, true));
        createResponse.EnsureSuccessStatusCode();

        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }
}
