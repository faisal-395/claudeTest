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

    /// <summary>
    /// Kachi/Pakki rates are quoted per Man (maund), matching arhti market convention — not per
    /// kg, even though weight is stored internally in kg. Converts a rate entered per Man into a
    /// gross amount using the same configurable Man→kg factor as everywhere else, so a change to
    /// that factor in Setup keeps rate entry and weight entry consistent.
    /// </summary>
    public static decimal GrossAmountFromRatePerMan(decimal ratePerMan, decimal netWeightKg, int? productId, IReadOnlyCollection<UnitConversion> conversions)
    {
        var manFactor = FactorFor(WeightUnit.Man, productId, conversions);
        var netWeightMan = netWeightKg / manFactor;
        return Math.Round(ratePerMan * netWeightMan, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Breaks a net weight down into whole Mans, whole Dhrns (a sub-Man denomination, 5kg by
    /// default) and a final Kg remainder — e.g. 52kg = 1 Man + 2 Dhrn + 2 Kg, using each unit's
    /// configured kg factor (Setup &gt; Unit Conversions). Purely a display breakdown: it never
    /// feeds GrossAmount, which is computed from the continuous net weight in
    /// GrossAmountFromRatePerMan, not from these rounded-down counts.
    /// </summary>
    public static (int ManCount, int DhrnCount, decimal KgRemainder) BreakdownIntoManDhrnKg(
        decimal netWeightKg, int? productId, IReadOnlyCollection<UnitConversion> conversions)
    {
        var manFactor = FactorFor(WeightUnit.Man, productId, conversions);
        var dhrnFactor = FactorFor(WeightUnit.Dhrn, productId, conversions);

        var manCount = (int)Math.Floor(netWeightKg / manFactor);
        var afterMan = netWeightKg - manCount * manFactor;

        var dhrnCount = (int)Math.Floor(afterMan / dhrnFactor);
        var kgRemainder = afterMan - dhrnCount * dhrnFactor;

        return (manCount, dhrnCount, kgRemainder);
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
