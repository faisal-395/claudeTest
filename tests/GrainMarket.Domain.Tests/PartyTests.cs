using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Xunit;

namespace GrainMarket.Domain.Tests;

public class PartyTests
{
    [Fact]
    public void NewParty_DefaultsToActiveAndDebitOpeningBalance()
    {
        var party = new Party { Name = "Test Farmer", PartyType = PartyType.Farmer };

        Assert.True(party.IsActive);
        Assert.Equal(BalanceSide.Debit, party.OpeningBalanceType);
        Assert.Equal(0m, party.OpeningBalance);
    }

    [Fact]
    public void NewKachi_DefaultsToOpenStatus()
    {
        var kachi = new Kachi();

        Assert.Equal(InvoiceStatus.Open, kachi.Status);
        Assert.Empty(kachi.DeductionLines);
    }
}
