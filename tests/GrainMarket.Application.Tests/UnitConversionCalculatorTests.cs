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
        new UnitConversion { Id = 4, Unit = WeightUnit.Bori, FactorToKg = 100m, IsActive = true }
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
}
