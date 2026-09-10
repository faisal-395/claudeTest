using GrainMarket.Application.DeductionRules;
using GrainMarket.Application.Kachis;
using GrainMarket.Application.MultiPurchase;
using GrainMarket.Application.Parties;
using GrainMarket.Domain.Enums;
using Xunit;

namespace GrainMarket.Application.Tests;

public class PartyValidatorTests
{
    private readonly UpsertPartyRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new UpsertPartyRequest("Ali Farms", "علی فارمز", PartyType.Farmer, "0300-1234567", "35202-1234567-1", "Village X", 0m, BalanceSide.Debit, true);
        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_EmptyName_Fails()
    {
        var request = new UpsertPartyRequest("", null, PartyType.Farmer, null, null, null, 0m, BalanceSide.Debit, true);
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_NegativeOpeningBalance_Fails()
    {
        var request = new UpsertPartyRequest("Ali Farms", null, PartyType.Farmer, null, null, null, -100m, BalanceSide.Debit, true);
        Assert.False(_validator.Validate(request).IsValid);
    }
}

public class CreateKachiRequestValidatorTests
{
    private readonly CreateKachiRequestValidator _validator = new();

    [Fact]
    public void Validate_NoBharti_Fails()
    {
        var request = new CreateKachiRequest(DateTime.Today, 1, 1, 1, 1, null, 3000m, null, 5000m, null, null);
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_NoTotalWeight_Fails()
    {
        var request = new CreateKachiRequest(DateTime.Today, 1, 1, 1, 1, 60m, null, null, 5000m, null, null);
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_BhartiTotalWeightAndRateEntered_Passes()
    {
        var request = new CreateKachiRequest(DateTime.Today, 1, 1, 1, 1, 60m, 3000m, null, 5000m, null, null);
        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_DhrnGreaterThanOrEqualToTotalWeight_Fails()
    {
        var request = new CreateKachiRequest(DateTime.Today, 1, 1, 1, 1, 60m, 3000m, 3000m, 5000m, null, null);
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_NegativeRate_Fails()
    {
        var request = new CreateKachiRequest(DateTime.Today, 1, 1, 1, 1, 60m, 3000m, null, -10m, null, null);
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_NoRate_Fails()
    {
        // Rate is required — Kachi no longer allows a purely provisional, unpriced weighing.
        var request = new CreateKachiRequest(DateTime.Today, 1, 1, 1, 1, 60m, 3000m, null, null, null, null);
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_NoBuyer_Fails()
    {
        // Buyer is required at Kachi stage.
        var request = new CreateKachiRequest(DateTime.Today, 1, 1, null, 1, 60m, 3000m, null, 5000m, null, null);
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_InvalidBuyerId_Fails()
    {
        var request = new CreateKachiRequest(DateTime.Today, 1, 1, 0, 1, 60m, 3000m, null, 5000m, null, null);
        Assert.False(_validator.Validate(request).IsValid);
    }
}

public class UpdateKachiRequestValidatorTests
{
    private readonly UpdateKachiRequestValidator _validator = new();

    [Fact]
    public void Validate_NoBuyer_Fails()
    {
        var request = new UpdateKachiRequest(DateTime.Today, 1, 1, null, 1, 60m, 3000m, null, 5000m, null, null);
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new UpdateKachiRequest(DateTime.Today, 1, 1, 1, 1, 60m, 3000m, null, 5000m, null, null);
        Assert.True(_validator.Validate(request).IsValid);
    }
}

public class CreateMultiPurchaseRequestValidatorTests
{
    private readonly CreateMultiPurchaseRequestValidator _validator = new();

    private static MultiPurchaseRowRequest ValidRow() => new(1, 1, 60m, 3000m, null, 5000m, null, null);

    [Fact]
    public void Validate_NoRows_Fails()
    {
        var request = new CreateMultiPurchaseRequest(DateTime.Today, 1, 1, new List<MultiPurchaseRowRequest>());
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_OneValidRow_Passes()
    {
        var request = new CreateMultiPurchaseRequest(DateTime.Today, 1, 1, new List<MultiPurchaseRowRequest> { ValidRow() });
        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_RowWithNoTotalWeight_Fails()
    {
        var badRow = new MultiPurchaseRowRequest(1, 1, 60m, null, null, 5000m, null, null);
        var request = new CreateMultiPurchaseRequest(DateTime.Today, 1, 1, new List<MultiPurchaseRowRequest> { ValidRow(), badRow });
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_RowWithNegativeRate_Fails()
    {
        var badRow = new MultiPurchaseRowRequest(1, 1, 60m, 3000m, null, -1m, null, null);
        var request = new CreateMultiPurchaseRequest(DateTime.Today, 1, 1, new List<MultiPurchaseRowRequest> { badRow });
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_RowWithNoRate_Fails()
    {
        var row = new MultiPurchaseRowRequest(1, 1, 60m, 3000m, null, null, null, null);
        var request = new CreateMultiPurchaseRequest(DateTime.Today, 1, 1, new List<MultiPurchaseRowRequest> { row });
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_RowWithDhrnEqualToTotalWeight_Fails()
    {
        var row = new MultiPurchaseRowRequest(1, 1, 60m, 3000m, 3000m, 5000m, null, null);
        var request = new CreateMultiPurchaseRequest(DateTime.Today, 1, 1, new List<MultiPurchaseRowRequest> { row });
        Assert.False(_validator.Validate(request).IsValid);
    }
}

public class DeductionRuleValidatorTests
{
    private readonly UpsertDeductionRuleRequestValidator _validator = new();

    [Fact]
    public void Validate_PercentOver100_Fails()
    {
        var request = new UpsertDeductionRuleRequest("Commission", "کمیشن", DeductionCalculationType.PercentOfGross, 150m, DeductionAppliesTo.Pakki, DeductionChargedTo.Farmer, 1, true, null, null, false, null);
        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_PercentWithin100_Passes()
    {
        var request = new UpsertDeductionRuleRequest("Commission", "کمیشن", DeductionCalculationType.PercentOfGross, 2m, DeductionAppliesTo.Pakki, DeductionChargedTo.Farmer, 1, true, null, null, false, null);
        Assert.True(_validator.Validate(request).IsValid);
    }
}
