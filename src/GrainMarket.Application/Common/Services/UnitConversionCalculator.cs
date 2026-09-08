using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Common.Services;

/// <summary>
/// Converts raw quantities entered in local units (Man/maund, Kilo, Gram, Bori/bag) into the
/// base storage unit (kg), using per-product conversion factors when present and falling back
/// to the global default for that unit. Pure/stateless: safe to unit test without a database.
/// </summary>
public static class UnitConversionCalculator
{
    public static decimal ToBaseKg(
        decimal? manQty,
        decimal? kiloQty,
        decimal? gramQty,
        decimal? boriQty,
        int? productId,
        IReadOnlyCollection<UnitConversion> conversions)
    {
        if (manQty is null && kiloQty is null && gramQty is null && boriQty is null)
        {
            throw new InvalidCalculationException("At least one weight quantity (Man/Kilo/Gram/Bori) must be entered.");
        }

        decimal total = 0m;

        if (manQty is > 0) total += manQty.Value * FactorFor(WeightUnit.Man, productId, conversions);
        if (kiloQty is > 0) total += kiloQty.Value * FactorFor(WeightUnit.Kilo, productId, conversions);
        if (gramQty is > 0) total += gramQty.Value * FactorFor(WeightUnit.Gram, productId, conversions);
        if (boriQty is > 0) total += boriQty.Value * FactorFor(WeightUnit.Bori, productId, conversions);

        if (total <= 0)
        {
            throw new InvalidCalculationException("Total weight must be greater than zero.");
        }

        return total;
    }

    public static decimal FactorFor(WeightUnit unit, int? productId, IReadOnlyCollection<UnitConversion> conversions)
    {
        var productSpecific = productId.HasValue
            ? conversions.FirstOrDefault(c => c.Unit == unit && c.ProductId == productId && c.IsActive)
            : null;

        var factorRow = productSpecific
            ?? conversions.FirstOrDefault(c => c.Unit == unit && c.ProductId == null && c.IsActive);

        if (factorRow is null || factorRow.FactorToKg <= 0)
        {
            throw new InvalidCalculationException($"No active conversion factor is configured for unit '{unit}'. Configure it under Setup > Unit Conversions.");
        }

        return factorRow.FactorToKg;
    }
}
