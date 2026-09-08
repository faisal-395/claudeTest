using System.Net.Http.Json;
using GrainMarket.Application.ChartOfAccounts;
using Xunit;

namespace GrainMarket.Api.Tests;

/// <summary>
/// Verifies protected chart-of-accounts rows (IsProtected = true) are filtered out server-side
/// for roles not on the account's AllowedRoleIds — not just hidden client-side. Seed data marks
/// "Owner's Equity" (3000) and "Withholding Tax Payable" (4500) as protected, visible only to
/// Owner/Admin (and Accountant for 4500).
/// </summary>
public class ProtectedAccountTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ProtectedAccountTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetChartOfAccounts_AsOwner_IncludesProtectedAccounts()
    {
        var owner = await TestAuthHelper.AsAdminAsync(_factory);

        var accounts = await owner.GetFromJsonAsync<List<ChartOfAccountDto>>("/api/chart-of-accounts");

        Assert.Contains(accounts!, a => a.Code == "3000");
        Assert.Contains(accounts!, a => a.Code == "4500");
    }

    [Fact]
    public async Task GetChartOfAccounts_AsManager_ExcludesProtectedAccounts()
    {
        var manager = await TestAuthHelper.AsNewUserWithRoleAsync(_factory, "Manager", "manager.protected.test");

        var accounts = await manager.GetFromJsonAsync<List<ChartOfAccountDto>>("/api/chart-of-accounts");

        Assert.DoesNotContain(accounts!, a => a.Code == "3000");
        Assert.DoesNotContain(accounts!, a => a.Code == "4500");
        // Non-protected accounts remain visible.
        Assert.Contains(accounts!, a => a.Code == "1000");
    }

    [Fact]
    public async Task GetSingleProtectedAccountById_AsManager_ReturnsNotFound()
    {
        var owner = await TestAuthHelper.AsAdminAsync(_factory);
        var accounts = await owner.GetFromJsonAsync<List<ChartOfAccountDto>>("/api/chart-of-accounts");
        var protectedAccountId = accounts!.Single(a => a.Code == "3000").Id;

        var manager = await TestAuthHelper.AsNewUserWithRoleAsync(_factory, "Manager", "manager.byid.test");
        var response = await manager.GetAsync($"/api/chart-of-accounts/{protectedAccountId}");

        // A raw id lookup must not leak the protected account's existence either.
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
