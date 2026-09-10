using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Application.Common.Services;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;
using Xunit;

namespace GrainMarket.Application.Tests;

public class UnitConversionCalculatorTests
{
    private static List<UnitConversion> DefaultConversions() => new()
    {
        new UnitConversion { Id = 1, Unit = WeightUnit.Kilo, FactorToKg = 1m, IsActive = true },
        new UnitConversion { Id = 2, Unit = WeightUnit.Gram, FactorToKg = 0.001m, IsActive = true },
        new UnitConversion { Id = 3, Unit = WeightUnit.Man, FactorToKg = 40m, IsActive = true },
        new UnitConversion { Id = 4, Unit = WeightUnit.Bori, FactorToKg = 100m, IsActive = true },
        new UnitConversion { Id = 5, Unit = WeightUnit.Dhrn, FactorToKg = 5m, IsActive = true }
    };

    [Fact]
    public void ToBaseKg_SingleManQuantity_ConvertsUsingDefaultFactor()
    {
        var result = UnitConversionCalculator.ToBaseKg(manQty: 2m, kiloQty: null, gramQty: null, boriQty: null, productId: null, DefaultConversions());

        Assert.Equal(80m, result);
    }

    [Fact]
    public void ToBaseKg_MixedUnits_SumsAllInKg()
    {
        // 1 Man (40kg) + 5 Kilo + 500 Gram (0.5kg) = 45.5 kg
        var result = UnitConversionCalculator.ToBaseKg(manQty: 1m, kiloQty: 5m, gramQty: 500m, boriQty: null, productId: null, DefaultConversions());

        Assert.Equal(45.5m, result);
    }

    [Fact]
    public void ToBaseKg_ProductSpecificFactor_OverridesGlobalDefault()
    {
        var conversions = DefaultConversions();
        conversions.Add(new UnitConversion { Id = 5, Unit = WeightUnit.Bori, FactorToKg = 50m, ProductId = 7, IsActive = true });

        var result = UnitConversionCalculator.ToBaseKg(manQty: null, kiloQty: null, gramQty: null, boriQty: 2m, productId: 7, conversions);

        Assert.Equal(100m, result); // 2 * 50kg (product-specific), not 2 * 100kg (global default)
    }

    [Fact]
    public void ToBaseKg_NoQuantitiesEntered_ThrowsInvalidCalculation()
    {
        Assert.Throws<InvalidCalculationException>(() =>
            UnitConversionCalculator.ToBaseKg(null, null, null, null, null, DefaultConversions()));
    }

    [Fact]
    public void ToBaseKg_MissingConversionFactor_ThrowsInvalidCalculation()
    {
        var conversions = new List<UnitConversion>(); // no factors configured at all

        Assert.Throws<InvalidCalculationException>(() =>
            UnitConversionCalculator.ToBaseKg(manQty: 1m, null, null, null, null, conversions));
    }

    [Fact]
    public void ToBaseKg_InactiveConversion_IsIgnored()
    {
        var conversions = new List<UnitConversion>
        {
            new() { Unit = WeightUnit.Man, FactorToKg = 40m, IsActive = false }
        };

        Assert.Throws<InvalidCalculationException>(() =>
            UnitConversionCalculator.ToBaseKg(manQty: 1m, null, null, null, null, conversions));
    }

    [Fact]
    public void GrossAmountFromRatePerMan_ComputesPerMaundNotPerKg()
    {
        // 10 Man (= 400 kg at the default 40kg/Man factor) at Rs 5000/Man should be Rs 50,000 —
        // not Rs 5000 * 400kg = Rs 2,000,000, which is what a naive rate-times-kg calculation
        // would produce if the rate were (wrongly) treated as per kg.
        var result = UnitConversionCalculator.GrossAmountFromRatePerMan(ratePerMan: 5000m, netWeightKg: 400m, productId: null, DefaultConversions());

        Assert.Equal(50000m, result);
    }

    [Fact]
    public void GrossAmountFromRatePerMan_UsesProductSpecificManFactorWhenSet()
    {
        var conversions = DefaultConversions();
        conversions.Add(new UnitConversion { Id = 5, Unit = WeightUnit.Man, FactorToKg = 37.5m, ProductId = 3, IsActive = true });

        // 75 kg at a 37.5kg/Man product-specific factor = 2 Man, at Rs 1000/Man = Rs 2000.
        var result = UnitConversionCalculator.GrossAmountFromRatePerMan(ratePerMan: 1000m, netWeightKg: 75m, productId: 3, conversions);

        Assert.Equal(2000m, result);
    }

    [Fact]
    public void GrossAmountFromRatePerMan_MissingManFactor_ThrowsInvalidCalculation()
    {
        var conversions = new List<UnitConversion>(); // no Man factor configured

        Assert.Throws<InvalidCalculationException>(() =>
            UnitConversionCalculator.GrossAmountFromRatePerMan(1000m, 400m, null, conversions));
    }

    [Fact]
    public void BreakdownIntoManDhrnKg_52Kg_Is1Man2Dhrn2Kg()
    {
        // 52kg = 1 Man (40kg) + 2 Dhrn (2 * 5kg = 10kg) + 2kg remainder.
        var (manCount, dhrnCount, kgRemainder) = UnitConversionCalculator.BreakdownIntoManDhrnKg(52m, productId: null, DefaultConversions());

        Assert.Equal(1, manCount);
        Assert.Equal(2, dhrnCount);
        Assert.Equal(2m, kgRemainder);
    }

    [Fact]
    public void BreakdownIntoManDhrnKg_ExactMultipleOfMan_HasNoDhrnOrRemainder()
    {
        var (manCount, dhrnCount, kgRemainder) = UnitConversionCalculator.BreakdownIntoManDhrnKg(80m, productId: null, DefaultConversions());

        Assert.Equal(2, manCount);
        Assert.Equal(0, dhrnCount);
        Assert.Equal(0m, kgRemainder);
    }

    [Fact]
    public void BreakdownIntoManDhrnKg_LessThanOneMan_HasZeroManCount()
    {
        // 17kg = 0 Man + 3 Dhrn (15kg) + 2kg remainder.
        var (manCount, dhrnCount, kgRemainder) = UnitConversionCalculator.BreakdownIntoManDhrnKg(17m, productId: null, DefaultConversions());

        Assert.Equal(0, manCount);
        Assert.Equal(3, dhrnCount);
        Assert.Equal(2m, kgRemainder);
    }

    [Fact]
    public void BreakdownIntoManDhrnKg_MissingDhrnFactor_ThrowsInvalidCalculation()
    {
        var conversions = DefaultConversions().Where(c => c.Unit != WeightUnit.Dhrn).ToList();

        Assert.Throws<InvalidCalculationException>(() =>
            UnitConversionCalculator.BreakdownIntoManDhrnKg(52m, null, conversions));
    }
}
