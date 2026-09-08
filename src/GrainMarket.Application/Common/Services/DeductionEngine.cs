using GrainMarket.Application.Common.Exceptions;
using GrainMarket.Domain.Entities;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Application.Common.Services;

public record DeductionLineResult(int DeductionRuleId, string Name, string NameUrdu, decimal Amount, bool RequiresVehicleNumber, int? IncomeAccountId);

public record DeductionCalculationResult(IReadOnlyList<DeductionLineResult> Lines, decimal TotalDeductions, decimal NetAmount);

/// <summary>
/// Computes the deduction lines (commission, market fee, association fund, octroi, withholding
/// tax, labour, bagging/stitching, freight, ...) for a Kachi or Pakki, and the resulting net
/// amount. Pure/stateless: safe to unit test without a database. Never returns NaN or an
/// unbounded result — every input is validated first.
/// </summary>
public static class DeductionEngine
{
    public static DeductionCalculationResult Calculate(
        decimal grossAmount,
        decimal netWeightKg,
        DeductionAppliesTo appliesTo,
        int? productId,
        int? partyId,
        IReadOnlyCollection<DeductionRule> allRules)
    {
        if (grossAmount < 0)
        {
            throw new InvalidCalculationException("Gross amount cannot be negative.");
        }

        if (netWeightKg <= 0)
        {
            throw new InvalidCalculationException("Net weight must be greater than zero before deductions can be calculated.");
        }

        var applicableRules = allRules
            .Where(r => r.IsActive)
            .Where(r => r.AppliesTo == appliesTo || r.AppliesTo == DeductionAppliesTo.Both)
            .Where(r => r.ProductId == null || r.ProductId == productId)
            .Where(r => r.PartyId == null || r.PartyId == partyId)
            .OrderBy(r => r.SortOrder)
            .ToList();

        var lines = new List<DeductionLineResult>();
        decimal total = 0m;

        foreach (var rule in applicableRules)
        {
            var amount = rule.CalculationType switch
            {
                DeductionCalculationType.FixedAmount => rule.Value,
                DeductionCalculationType.PercentOfGross => Math.Round(grossAmount * (rule.Value / 100m), 2, MidpointRounding.AwayFromZero),
                DeductionCalculationType.PerUnitWeight => Math.Round(rule.Value * netWeightKg, 2, MidpointRounding.AwayFromZero),
                _ => throw new InvalidCalculationException($"Unknown deduction calculation type '{rule.CalculationType}'.")
            };

            if (decimal.IsNegative(amount) || amount == decimal.MaxValue)
            {
                throw new InvalidCalculationException($"Deduction '{rule.Name}' produced an invalid amount.");
            }

            lines.Add(new DeductionLineResult(rule.Id, rule.Name, rule.NameUrdu, amount, rule.RequiresVehicleNumber, rule.IncomeAccountId));
            total += amount;
        }

        var net = grossAmount - total;
        return new DeductionCalculationResult(lines, total, net);
    }
}
