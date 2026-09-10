using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Services;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Xunit;

namespace GrainMarket.Application.Tests;

public class DeductionEngineTests
{
    [Fact]
    public void Calculate_PercentOfGross_ComputesCorrectAmount()
    {
        var rules = new List<DeductionRule>
        {
            new() { Id = 1, Name = "Commission", NameUrdu = "کمیشن", CalculationType = DeductionCalculationType.PercentOfGross, Value = 2m, AppliesTo = DeductionAppliesTo.Pakki, IsActive = true }
        };

        var result = DeductionEngine.Calculate(10000m, 400m, DeductionAppliesTo.Pakki, productId: null, partyId: null, rules);

        Assert.Equal(200m, result.TotalDeductions);
        Assert.Equal(9800m, result.NetAmount);
        Assert.Single(result.Lines);
    }

    [Fact]
    public void Calculate_PerUnitWeight_MultipliesByNetWeight()
    {
        var rules = new List<DeductionRule>
        {
            new() { Id = 1, Name = "Labour", NameUrdu = "پلیداری", CalculationType = DeductionCalculationType.PerUnitWeight, Value = 0.3m, AppliesTo = DeductionAppliesTo.Both, IsActive = true }
        };

        var result = DeductionEngine.Calculate(5000m, 400m, DeductionAppliesTo.Kachi, null, null, rules);

        Assert.Equal(120m, result.TotalDeductions); // 0.3 * 400
    }

    [Fact]
    public void Calculate_FixedAmount_IgnoresGrossAndWeight()
    {
        var rules = new List<DeductionRule>
        {
            new() { Id = 1, Name = "Association Fund", NameUrdu = "انجمن فنڈ", CalculationType = DeductionCalculationType.FixedAmount, Value = 10m, AppliesTo = DeductionAppliesTo.Both, IsActive = true }
        };

        var result = DeductionEngine.Calculate(1m, 1m, DeductionAppliesTo.Pakki, null, null, rules);

        Assert.Equal(10m, result.TotalDeductions);
    }

    [Fact]
    public void Calculate_MultipleRulesAcrossTypes_SumsCorrectly()
    {
        var rules = new List<DeductionRule>
        {
            new() { Id = 1, Name = "Commission", NameUrdu = "کمیشن", CalculationType = DeductionCalculationType.PercentOfGross, Value = 2m, AppliesTo = DeductionAppliesTo.Pakki, SortOrder = 1, IsActive = true },
            new() { Id = 2, Name = "Labour", NameUrdu = "پلیداری", CalculationType = DeductionCalculationType.PerUnitWeight, Value = 0.3m, AppliesTo = DeductionAppliesTo.Both, SortOrder = 2, IsActive = true },
            new() { Id = 3, Name = "Association Fund", NameUrdu = "انجمن فنڈ", CalculationType = DeductionCalculationType.FixedAmount, Value = 10m, AppliesTo = DeductionAppliesTo.Pakki, SortOrder = 3, IsActive = true }
        };

        var result = DeductionEngine.Calculate(10000m, 400m, DeductionAppliesTo.Pakki, null, null, rules);

        // 200 (commission) + 120 (labour) + 10 (assoc fund) = 330
        Assert.Equal(330m, result.TotalDeductions);
        Assert.Equal(9670m, result.NetAmount);
        Assert.Equal(3, result.Lines.Count);
    }

    [Fact]
    public void Calculate_RuleForDifferentAppliesTo_IsExcluded()
    {
        var rules = new List<DeductionRule>
        {
            new() { Id = 1, Name = "Commission", NameUrdu = "کمیشن", CalculationType = DeductionCalculationType.PercentOfGross, Value = 2m, AppliesTo = DeductionAppliesTo.Pakki, IsActive = true }
        };

        var result = DeductionEngine.Calculate(10000m, 400m, DeductionAppliesTo.Kachi, null, null, rules);

        Assert.Empty(result.Lines);
        Assert.Equal(0m, result.TotalDeductions);
        Assert.Equal(10000m, result.NetAmount);
    }

    [Fact]
    public void Calculate_ProductSpecificRule_OnlyAppliesToThatProduct()
    {
        var rules = new List<DeductionRule>
        {
            new() { Id = 1, Name = "Rice Levy", NameUrdu = "چاول محصول", CalculationType = DeductionCalculationType.FixedAmount, Value = 50m, AppliesTo = DeductionAppliesTo.Both, ProductId = 99, IsActive = true }
        };

        var forRice = DeductionEngine.Calculate(1000m, 100m, DeductionAppliesTo.Both, productId: 99, partyId: null, rules);
        var forWheat = DeductionEngine.Calculate(1000m, 100m, DeductionAppliesTo.Both, productId: 1, partyId: null, rules);

        Assert.Single(forRice.Lines);
        Assert.Empty(forWheat.Lines);
    }

    [Fact]
    public void Calculate_ZeroNetWeight_ThrowsInvalidCalculation()
    {
        Assert.Throws<InvalidCalculationException>(() =>
            DeductionEngine.Calculate(1000m, 0m, DeductionAppliesTo.Pakki, null, null, new List<DeductionRule>()));
    }

    [Fact]
    public void Calculate_NegativeGrossAmount_ThrowsInvalidCalculation()
    {
        Assert.Throws<InvalidCalculationException>(() =>
            DeductionEngine.Calculate(-1m, 100m, DeductionAppliesTo.Pakki, null, null, new List<DeductionRule>()));
    }

    [Fact]
    public void Calculate_ChargedTo_IsPassedThroughPerLine_ButDoesNotAffectTotals()
    {
        // TotalDeductions/NetAmount stay unfiltered by ChargedTo — callers (KachiService) split by
        // line.ChargedTo themselves; Pakki, which never filters, must see identical totals either way.
        var rules = new List<DeductionRule>
        {
            new() { Id = 1, Name = "Aarat", NameUrdu = "آڑت", CalculationType = DeductionCalculationType.PercentOfGross, Value = 1.6m, AppliesTo = DeductionAppliesTo.Kachi, ChargedTo = DeductionChargedTo.Farmer, IsActive = true },
            new() { Id = 2, Name = "Commission", NameUrdu = "کمیشن", CalculationType = DeductionCalculationType.PercentOfGross, Value = 1m, AppliesTo = DeductionAppliesTo.Kachi, ChargedTo = DeductionChargedTo.Buyer, IsActive = true }
        };

        var result = DeductionEngine.Calculate(10000m, 400m, DeductionAppliesTo.Kachi, null, null, rules);

        Assert.Equal(2, result.Lines.Count);
        Assert.Equal(DeductionChargedTo.Farmer, result.Lines.Single(l => l.DeductionRuleId == 1).ChargedTo);
        Assert.Equal(DeductionChargedTo.Buyer, result.Lines.Single(l => l.DeductionRuleId == 2).ChargedTo);
        Assert.Equal(260m, result.TotalDeductions); // 160 (aarat) + 100 (commission) — unfiltered
    }

    [Fact]
    public void Calculate_InactiveRule_IsExcluded()
    {
        var rules = new List<DeductionRule>
        {
            new() { Id = 1, Name = "Old Fee", NameUrdu = "پرانی فیس", CalculationType = DeductionCalculationType.FixedAmount, Value = 100m, AppliesTo = DeductionAppliesTo.Both, IsActive = false }
        };

        var result = DeductionEngine.Calculate(1000m, 100m, DeductionAppliesTo.Both, null, null, rules);

        Assert.Empty(result.Lines);
    }
}
