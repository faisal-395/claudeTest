using System.Net.Http.Json;
using GrainMarket.Application.Kachis;
using GrainMarket.Application.Ledger;
using GrainMarket.Application.Parties;
using GrainMarket.Application.Products;
using GrainMarket.Application.Seasons;
using GrainMarket.Domain.Enums;
using Xunit;

namespace GrainMarket.Api.Tests;

/// <summary>
/// A Kachi must post to the ledger the moment it's created — the buyer owes GrossAmount plus
/// whatever's charged to them, the farmer is owed GrossAmount minus what's charged to them. Seed
/// data's Kachi-stage rules (Aarat, Labour, Broker) are all farmer-charged, so with no
/// buyer-charged rule configured, BuyerChargesTotal is 0 and the buyer's debit equals GrossAmount.
/// </summary>
public class KachiLedgerPostingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public KachiLedgerPostingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateKachi_PostsFarmerAndBuyerLedgerEntries()
    {
        var admin = await TestAuthHelper.AsAdminAsync(_factory);

        var farmer = await CreatePartyAsync(admin, PartyType.Farmer, "Ledger Test Farmer");
        var buyer = await CreatePartyAsync(admin, PartyType.Buyer, "Ledger Test Buyer");
        var season = (await admin.GetFromJsonAsync<List<SeasonDto>>("/api/seasons"))!.First();
        var product = (await admin.GetFromJsonAsync<List<ProductDto>>("/api/products"))!.First();

        var request = new CreateKachiRequest(
            DateTime.Today, season.Id, farmer.Id, buyer.Id, product.Id,
            BhartiKgPerBag: 60m, TotalWeightKg: 3000m, DhrnKg: null,
            RatePerUnit: 2000m, VehicleNumber: null, Notes: null);

        var createResponse = await admin.PostAsJsonAsync("/api/kachis", request);
        createResponse.EnsureSuccessStatusCode();
        var kachi = await createResponse.Content.ReadFromJsonAsync<KachiDto>();

        var farmerLedger = await admin.GetFromJsonAsync<PartyLedgerDto>($"/api/ledger/party/{farmer.Id}");
        var buyerLedger = await admin.GetFromJsonAsync<PartyLedgerDto>($"/api/ledger/party/{buyer.Id}");

        var farmerRow = Assert.Single(farmerLedger!.Rows, r => r.SourceType == LedgerSourceType.Kachi && r.SourceId == kachi!.Id);
        Assert.Equal(0m, farmerRow.Debit);
        Assert.Equal(kachi!.Total, farmerRow.Credit);

        var buyerRow = Assert.Single(buyerLedger!.Rows, r => r.SourceType == LedgerSourceType.Kachi && r.SourceId == kachi.Id);
        Assert.Equal(kachi.GrossAmount + kachi.BuyerChargesTotal, buyerRow.Debit);
        Assert.Equal(0m, buyerRow.Credit);
    }

    [Fact]
    public async Task CreateKachi_NoDhrnEntered_DefaultsToFiveKg()
    {
        var admin = await TestAuthHelper.AsAdminAsync(_factory);

        var farmer = await CreatePartyAsync(admin, PartyType.Farmer, "Ledger Default Dhrn Farmer");
        var buyer = await CreatePartyAsync(admin, PartyType.Buyer, "Ledger Default Dhrn Buyer");
        var season = (await admin.GetFromJsonAsync<List<SeasonDto>>("/api/seasons"))!.First();
        var product = (await admin.GetFromJsonAsync<List<ProductDto>>("/api/products"))!.First();

        var request = new CreateKachiRequest(
            DateTime.Today, season.Id, farmer.Id, buyer.Id, product.Id,
            BhartiKgPerBag: 60m, TotalWeightKg: 1000m, DhrnKg: null,
            RatePerUnit: 2000m, VehicleNumber: null, Notes: null);

        var createResponse = await admin.PostAsJsonAsync("/api/kachis", request);
        createResponse.EnsureSuccessStatusCode();
        var kachi = await createResponse.Content.ReadFromJsonAsync<KachiDto>();

        Assert.Equal(5m, kachi!.DhrnKg);
        Assert.Equal(995m, kachi.NetWeightKg); // Safi Wazan = 1000 - 5
    }

    [Fact]
    public async Task CancelKachi_ReversesLedgerEntries()
    {
        var admin = await TestAuthHelper.AsAdminAsync(_factory);

        var farmer = await CreatePartyAsync(admin, PartyType.Farmer, "Ledger Cancel Farmer");
        var buyer = await CreatePartyAsync(admin, PartyType.Buyer, "Ledger Cancel Buyer");
        var season = (await admin.GetFromJsonAsync<List<SeasonDto>>("/api/seasons"))!.First();
        var product = (await admin.GetFromJsonAsync<List<ProductDto>>("/api/products"))!.First();

        var request = new CreateKachiRequest(
            DateTime.Today, season.Id, farmer.Id, buyer.Id, product.Id,
            BhartiKgPerBag: 65m, TotalWeightKg: 2000m, DhrnKg: 20m,
            RatePerUnit: 3000m, VehicleNumber: null, Notes: null);

        var createResponse = await admin.PostAsJsonAsync("/api/kachis", request);
        createResponse.EnsureSuccessStatusCode();
        var kachi = await createResponse.Content.ReadFromJsonAsync<KachiDto>();

        var cancelResponse = await admin.PostAsync($"/api/kachis/{kachi!.Id}/cancel", null);
        cancelResponse.EnsureSuccessStatusCode();

        var farmerLedger = await admin.GetFromJsonAsync<PartyLedgerDto>($"/api/ledger/party/{farmer.Id}");
        var kachiRows = farmerLedger!.Rows.Where(r => r.SourceType == LedgerSourceType.Kachi && r.SourceId == kachi.Id).ToList();

        Assert.Equal(2, kachiRows.Count); // original post + reversal
        Assert.Equal(0m, farmerLedger.ClosingBalance); // a brand-new party with no other activity nets back to zero
    }

    private static async Task<PartyDto> CreatePartyAsync(HttpClient client, PartyType type, string name)
    {
        var request = new UpsertPartyRequest(name, null, type, null, null, null, 0m, BalanceSide.Debit, true);
        var response = await client.PostAsJsonAsync("/api/parties", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PartyDto>())!;
    }
}
