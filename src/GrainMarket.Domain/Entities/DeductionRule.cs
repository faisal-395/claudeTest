using GrainMarket.Domain.Common;
using GrainMarket.Domain.Enums;

namespace GrainMarket.Domain.Entities;

/// <summary>
/// Setup &gt; Format: a configurable deduction (commission, market fee, association fund, octroi,
/// withholding tax, labour, bagging/stitching, freight, ...) applied to a Kachi and/or Pakki.
/// Seeded from the legacy app's fixed values, but editable here.
/// </summary>
public class DeductionRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NameUrdu { get; set; } = string.Empty;
    public DeductionCalculationType CalculationType { get; set; }

    /// <summary>Fixed rupee amount, percentage (0-100), or rupees-per-base-unit depending on CalculationType.</summary>
    public decimal Value { get; set; }

    public DeductionAppliesTo AppliesTo { get; set; }

    /// <summary>Farmer (default, existing behavior — reduces what the farmer receives) or Buyer
    /// (calculated and shown, but never reduces the farmer's payable). See DeductionChargedTo.</summary>
    public DeductionChargedTo ChargedTo { get; set; } = DeductionChargedTo.Seller;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Optional: restrict this rule to a single product. Null = applies to all products.</summary>
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Optional: restrict this rule to a single party (e.g. a special-rate farmer). Null = applies to all.</summary>
    public int? PartyId { get; set; }
    public Party? Party { get; set; }

    /// <summary>True for the Freight deduction, which also captures a vehicle number on the transaction.</summary>
    public bool RequiresVehicleNumber { get; set; }

    /// <summary>Chart-of-accounts row this deduction is credited to when a Kachi/Pakki posts to the
    /// ledger, so the buyer/farmer double-entry always balances. Falls back to the seeded
    /// "Unallocated Deductions" suspense account when left unset.</summary>
    public int? IncomeAccountId { get; set; }
    public ChartOfAccount? IncomeAccount { get; set; }
}
